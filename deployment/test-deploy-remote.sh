#!/usr/bin/env bash
set -Eeuo pipefail

source_script=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/deploy-remote.sh
test_root=$(mktemp -d /tmp/yuudachi-deploy-tests.XXXXXXXX)
stages=()
cleanup() {
    local stage
    for stage in "${stages[@]}"; do
        rm -f -- "$stage/deploy-remote.sh" "$stage/publish.tar.gz"
        if [[ -d $stage ]]; then rmdir -- "$stage"; fi
    done
    rm -r -- "$test_root"
}
trap cleanup EXIT
mkdir "$test_root/tools"
cat >"$test_root/tools/systemctl" <<'MOCK'
#!/usr/bin/env bash
set -eu
printf '%s\n' "$1" >>"$TEST_STATE/calls"
case "$1" in
    show)
        case "$3" in
            --property=ExecStart) cat "$TEST_STATE/exec-start" ;;
            --property=MainPID)
                count=$(cat "$TEST_STATE/pid-checks")
                count=$((count + 1))
                echo "$count" >"$TEST_STATE/pid-checks"
                if [[ -f $TEST_STATE/restart && $count == 2 ]]; then
                    echo 200 >"$TEST_STATE/pid"
                    rm "$TEST_STATE/restart"
                fi
                cat "$TEST_STATE/pid"
                ;;
            *) exit 2 ;;
        esac
        ;;
    stop) echo inactive >"$TEST_STATE/status" ;;
    start)
        if [[ -f $TEST_STATE/fail-start ]]; then
            rm "$TEST_STATE/fail-start"
            exit 1
        fi
        echo active >"$TEST_STATE/status"
        echo 100 >"$TEST_STATE/pid"
        ;;
    is-active) [[ $(cat "$TEST_STATE/status") == active ]] ;;
    *) exit 2 ;;
esac
MOCK
cat >"$test_root/tools/uname" <<'MOCK'
#!/usr/bin/env bash
case "$1" in -s) echo Linux ;; -m) echo aarch64 ;; *) exit 2 ;; esac
MOCK
cat >"$test_root/tools/sleep" <<'MOCK'
#!/usr/bin/env bash
exit 0
MOCK
chmod +x "$test_root/tools/"*
export PATH="$test_root/tools:$PATH"

fail() { printf 'FAIL: %s\n' "$*" >&2; exit 1; }
prepare() {
    case_dir="$test_root/$1"
    install_dir="$case_dir/install"
    deploy_root="$case_dir/deployment"
    export TEST_STATE="$case_dir/state"
    mkdir -p "$install_dir" "$TEST_STATE" "$case_dir/payload"
    printf 'old executable\n' >"$install_dir/yuudachi"
    printf '{"source":"server"}\n' >"$install_dir/appsettings.json"
    printf '{"source":"server-user"}\n' >"$install_dir/usersettings.json"
    chmod 600 "$install_dir/appsettings.json"
    printf 'new executable\n' >"$case_dir/payload/yuudachi"
    printf '%s/yuudachi\n' "$install_dir" >"$TEST_STATE/exec-start"
    echo active >"$TEST_STATE/status"
    echo 100 >"$TEST_STATE/pid"
    echo 0 >"$TEST_STATE/pid-checks"
    : >"$TEST_STATE/calls"
}
upload() {
    stage=$(mktemp -d /tmp/yuudachi-deploy.XXXXXXXX)
    stages+=("$stage")
    cp "$source_script" "$stage/deploy-remote.sh"
    tar -czf "$stage/publish.tar.gz" -C "$case_dir/payload" .
}
deploy() {
    bash "$stage/deploy-remote.sh" yuudachi.service "$install_dir" "$deploy_root" "$1" 5 >"$case_dir/output" 2>&1
}

prepare first-deployment
upload
release_id=20261027T120000Z-aaaaaaaa
deploy "$release_id" || { cat "$case_dir/output"; fail 'First deployment failed.'; }
[[ -L $install_dir && $(readlink "$install_dir") == "$deploy_root/releases/$release_id" ]] || fail 'Installation was not switched to the new release.'
[[ $(cat "$install_dir/yuudachi") == 'new executable' && -x $install_dir/yuudachi ]] || fail 'Executable content or permissions are wrong.'
[[ $(cat "$install_dir.previous-$release_id/yuudachi") == 'old executable' ]] || fail 'Original installation was not retained.'
[[ -L $install_dir/appsettings.json && $(stat -c %a "$deploy_root/shared/appsettings.json") == 600 ]] || fail 'Settings permissions or link were not preserved.'
[[ $(cat "$install_dir/usersettings.json") == '{"source":"server-user"}' ]] || fail 'User settings were not preserved.'
printf '{"source":"edited-on-server"}\n' >"$deploy_root/shared/appsettings.json"
upload
second_release=20261027T120001Z-bbbbbbbb
deploy "$second_release" || { cat "$case_dir/output"; fail 'Second deployment failed.'; }
[[ $(readlink "$install_dir") == "$deploy_root/releases/$second_release" ]] || fail 'Second release was not activated.'
[[ $(cat "$install_dir/appsettings.json") == '{"source":"edited-on-server"}' ]] || fail 'Shared settings were overwritten.'
[[ -d $deploy_root/releases/$release_id ]] || fail 'Previous release was deleted.'
printf 'PASS: initial migration, repeated deployment, permissions, and shared configuration\n'

prepare failed-start
touch "$TEST_STATE/fail-start"
upload
if deploy 20261027T120002Z-cccccccc; then fail 'Startup failure was reported as success.'; fi
[[ -d $install_dir && ! -L $install_dir && $(cat "$install_dir/yuudachi") == 'old executable' ]] || fail 'Original directory was not restored.'
[[ $(cat "$TEST_STATE/status") == active ]] || fail 'Original service was not restarted.'
printf 'PASS: failed start restores original directory and running service\n'

prepare restarted-process
mv "$install_dir" "$case_dir/old-release"
ln -s "$case_dir/old-release" "$install_dir"
touch "$TEST_STATE/restart"
upload
if deploy 20261027T120003Z-dddddddd; then fail 'Restarting process was accepted as healthy.'; fi
[[ $(readlink "$install_dir") == "$case_dir/old-release" && $(cat "$TEST_STATE/status") == active ]] || fail 'Previous symlink or running service was not restored.'
printf 'PASS: process restart triggers rollback to previous symlink\n'

prepare unwanted-settings
echo '{}' >"$case_dir/payload/appsettings.json"
upload
if deploy 20261027T120004Z-eeeeeeee; then fail 'An archive containing settings was accepted.'; fi
if grep -qx stop "$TEST_STATE/calls"; then fail 'Service stopped before archive validation.'; fi
[[ $(cat "$install_dir/yuudachi") == 'old executable' ]] || fail 'Preflight failure changed the installation.'
printf 'PASS: local settings are rejected before stopping the service\n'

prepare originally-stopped
echo inactive >"$TEST_STATE/status"
touch "$TEST_STATE/fail-start"
upload
if deploy 20261027T120005Z-ffffffff; then fail 'Failed start was reported as success.'; fi
[[ $(cat "$TEST_STATE/status") == inactive && ! -L $install_dir ]] || fail 'Rollback did not preserve the stopped state.'
printf 'PASS: rollback preserves an originally stopped service\n'

prepare wrong-unit
echo /somewhere/else/yuudachi >"$TEST_STATE/exec-start"
upload
if deploy 20261027T120006Z-aaaaaaaa; then fail 'Mismatched systemd unit was accepted.'; fi
if grep -qx stop "$TEST_STATE/calls"; then fail 'Mismatched unit caused a service stop.'; fi
printf 'PASS: mismatched unit is rejected before stopping the service\n'

prepare locked-deployment
mkdir -p "$deploy_root"
exec 8>"$deploy_root/deploy.lock"
flock -n 8
upload
if deploy 20261027T120007Z-bbbbbbbb; then fail 'Concurrent deployment lock was ignored.'; fi
exec 8>&-
if grep -qx stop "$TEST_STATE/calls"; then fail 'Locked deployment stopped the service.'; fi
printf 'PASS: concurrent deployment is rejected without stopping the service\n'

prepare environment-only
rm -- "$install_dir/appsettings.json" "$install_dir/usersettings.json"
upload
deploy 20261027T120008Z-cccccccc || { cat "$case_dir/output"; fail 'Deployment without JSON configuration failed.'; }
[[ -L $install_dir && ! -e $install_dir/appsettings.json && ! -e $install_dir/usersettings.json ]] || fail 'Environment-only deployment unexpectedly created settings.'
printf 'PASS: environment-only configuration does not require JSON files\n'
