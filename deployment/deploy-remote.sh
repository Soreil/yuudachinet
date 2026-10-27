#!/usr/bin/env bash
set -Eeuo pipefail
umask 022

fail() { printf '%s\n' "$*" >&2; exit 1; }
[[ $# == 5 ]] || fail 'Expected service, installation directory, deployment root, release ID, and startup check seconds.'
service=$1
install_dir=$2
deploy_root=$3
release_id=$4
check_seconds=$5
[[ $service =~ ^[A-Za-z0-9_][A-Za-z0-9_.@-]*\.service$ ]] || fail 'Invalid service name.'
[[ $release_id =~ ^[0-9]{8}T[0-9]{6}Z-[a-f0-9]{8}$ ]] || fail 'Invalid release ID.'
[[ $check_seconds =~ ^[0-9]+$ ]] && (( check_seconds >= 5 && check_seconds <= 300 )) || fail 'Invalid startup check duration.'
for path in "$install_dir" "$deploy_root"; do
    [[ $path =~ ^/[A-Za-z0-9_-]+(/[A-Za-z0-9_.-]+)+$ && ! $path =~ /\.\.?(/|$) ]] || fail 'Invalid deployment path.'
done
[[ $install_dir != "$deploy_root" && $install_dir != "$deploy_root/"* && $deploy_root != "$install_dir/"* ]] || fail 'Deployment paths must not overlap.'

stage=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
[[ $stage =~ ^/tmp/yuudachi-deploy\.[A-Za-z0-9]+$ ]] || fail 'Run this script from its temporary upload directory using Deploy.ps1.'
activation_started=0
switched=0
was_active=0
previous_target=''
legacy_backup=''
next_link="$install_dir.next-$release_id"

finish() {
    local status=$? rollback_failed=0
    trap - EXIT HUP INT TERM
    set +e
    if (( status != 0 && activation_started )); then
        printf 'Deployment failed; restoring the previous installation.\n' >&2
        systemctl stop "$service" || rollback_failed=1
        if [[ -n $legacy_backup && -d $legacy_backup ]]; then
            if [[ -L $install_dir ]]; then
                rm -- "$install_dir" || rollback_failed=1
            fi
            mv -T -- "$legacy_backup" "$install_dir" || rollback_failed=1
        elif (( switched )); then
            ln -s -- "$previous_target" "$next_link" && mv -Tf -- "$next_link" "$install_dir" || rollback_failed=1
        fi
        if (( was_active )); then
            systemctl start "$service" && systemctl is-active --quiet "$service" || rollback_failed=1
        fi
        if (( rollback_failed )); then
            printf 'ROLLBACK FAILED. Inspect %s and systemctl status %s immediately.\n' "$install_dir" "$service" >&2
        else
            printf 'Previous installation and service state restored.\n' >&2
        fi
        printf 'Inspect the failure with: journalctl -u %s -n 50 --no-pager\n' "$service" >&2
    fi
    if [[ -L $next_link ]]; then
        rm -- "$next_link" || printf 'Could not remove temporary symlink %s\n' "$next_link" >&2
    fi
    rm -f -- "$stage/publish.tar.gz" "$stage/deploy-remote.sh" && rmdir -- "$stage" || printf 'Could not completely clean staging directory %s\n' "$stage" >&2
    exit "$status"
}
trap finish EXIT
trap 'exit 130' INT
trap 'exit 143' HUP TERM

[[ $(uname -s) == Linux ]] || fail 'The server must run Linux.'
case $(uname -m) in aarch64|arm64) ;; *) fail 'The server must be ARM64.' ;; esac
for tool in systemctl flock tar readlink; do
    command -v "$tool" >/dev/null || fail "Required server command is missing: $tool"
done
[[ -d $install_dir && -f $install_dir/yuudachi ]] || fail "Expected an existing installation DIRECTORY containing $install_dir/yuudachi. The service has not been stopped."
[[ -f $stage/publish.tar.gz ]] || fail 'The uploaded archive is missing.'
exec_start=$(systemctl show "$service" --property=ExecStart --value)
[[ $exec_start == *"$install_dir/yuudachi"* ]] || fail "The service must launch Yuudachi from $install_dir. Inspect systemctl cat $service before deploying."

mkdir -p -- "$deploy_root"
exec 9>"$deploy_root/deploy.lock"
flock -n 9 || fail 'Another deployment is already in progress.'
mkdir -p -- "$deploy_root/releases" "$deploy_root/shared"
release="$deploy_root/releases/$release_id"
mkdir -- "$release"
tar -xzf "$stage/publish.tar.gz" -C "$release" --no-same-owner
[[ -f $release/yuudachi && ! -L $release/yuudachi ]] || fail 'The release is missing the Yuudachi executable.'
chmod 755 -- "$release" "$release/yuudachi"

shopt -s nullglob
for config in "$release"/appsettings*.json "$release"/usersettings.json; do
    [[ ! -e $config && ! -L $config ]] || fail 'The archive contains settings; refusing to overwrite server configuration.'
done
for config in "$install_dir"/appsettings*.json "$install_dir"/usersettings.json; do
    [[ -f $config ]] || continue
    shared="$deploy_root/shared/${config##*/}"
    if [[ ! -e $shared ]]; then
        cp -pL -- "$config" "$shared"
    fi
done
for config in "$deploy_root/shared"/appsettings*.json "$deploy_root/shared"/usersettings.json; do
    if [[ ! -e $config && ! -L $config ]]; then continue; fi
    [[ -f $config ]] || fail "Invalid shared configuration file: $config"
    ln -s -- "$config" "$release/${config##*/}"
done

if [[ -L $install_dir ]]; then
    previous_target=$(readlink -- "$install_dir")
fi
if systemctl is-active --quiet "$service"; then
    was_active=1
fi
ln -s -- "$release" "$next_link"
printf 'Activating release %s...\n' "$release_id"
activation_started=1
systemctl stop "$service"
if [[ ! -L $install_dir ]]; then
    backup="$install_dir.previous-$release_id"
    [[ ! -e $backup && ! -L $backup ]] || fail "Backup already exists: $backup"
    mv -T -- "$install_dir" "$backup"
    legacy_backup=$backup
fi
mv -Tf -- "$next_link" "$install_dir"
switched=1
systemctl start "$service"
main_pid=$(systemctl show "$service" --property=MainPID --value)
[[ $main_pid =~ ^[1-9][0-9]*$ ]] || fail 'The service did not start a process.'
for (( second=0; second<check_seconds; second++ )); do
    sleep 1
    systemctl is-active --quiet "$service" || fail 'The service stopped during startup verification.'
    [[ $(systemctl show "$service" --property=MainPID --value) == "$main_pid" ]] || fail 'The service restarted during startup verification.'
done
printf 'Deployment succeeded: %s stayed active for %s seconds.\n' "$service" "$check_seconds"
printf 'Current release: %s\nShared configuration: %s/shared\n' "$release" "$deploy_root"
printf 'Previous installation: %s\n' "${legacy_backup:-$previous_target}"