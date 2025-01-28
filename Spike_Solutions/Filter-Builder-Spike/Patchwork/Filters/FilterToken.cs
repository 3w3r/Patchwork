namespace Patchwork.Filters
{
  // public class FilterToken
  // {
  //   public FilterTokenType Type { get; set; }
  //   public string Value { get; set; } = string.Empty;
  // }

  public record FilterToken(FilterTokenType Type, string EntityName, string Value);
}
