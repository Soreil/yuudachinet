using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record ThreadDTO(
    [property: JsonPropertyName("no")] int Number,
//MM/DD/YY(Day)HH:MM (:SS on some boards), EST/EDT timezone	
    [property: JsonPropertyName("now")] string Now,
    [property: JsonPropertyName("sub")] string Subject,
    [property: JsonPropertyName("com")] string Comment,
    [property: JsonPropertyName("filename")] string FileName,
    [property: JsonPropertyName("ext")] string Extension,
    [property: JsonPropertyName("w")] int Width,
    [property: JsonPropertyName("h")] int Height,
    [property: JsonPropertyName("tn_w")] int ThumbnailWidth,
    [property: JsonPropertyName("tn_h")] int ThumbnailHeight,
//Unix timestamp + microtime that an image was uploaded	
    [property: JsonPropertyName("tim")] long Tim,
//UNIX timestamp the post was created	
    [property: JsonPropertyName("time")] int Time,
//24 character, packed base64 MD5 hash of file	
    [property: JsonPropertyName("md5")] string MD5,
    [property: JsonPropertyName("fsize")] int FileSize,
//For replies: this is the ID of the thread being replied to. For OP: this value is zero	
    [property: JsonPropertyName("resto")] int ThreadID,
    [property: JsonPropertyName("bumplimit")] int BumpLimit,
    [property: JsonPropertyName("imagelimit")] int ImageLimit,
    [property: JsonPropertyName("semantic_url")] string SemanticURL,
    [property: JsonPropertyName("replies")] int Replies,
    [property: JsonPropertyName("images")] int Images,
    [property: JsonPropertyName("omitted_posts")] int OmittedPosts,
    [property: JsonPropertyName("omitted_images")] int OmittedImages,
    [property: JsonPropertyName("last_replies")] List<ReplyDTO> LastReplies,
    [property: JsonPropertyName("last_modified")] int LastModified);
