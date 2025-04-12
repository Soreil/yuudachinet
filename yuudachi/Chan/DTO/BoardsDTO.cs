using System.Text.Json.Serialization;

namespace yuudachi.Chan.DTO;

public record BoardsDTO([property: JsonPropertyName("boards")] List<BoardDTO> Boards);
