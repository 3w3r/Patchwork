namespace Patchwork.Filters;

public record FilterToken(FilterTokenType Type, string EntityName, string Value, string ParameterName);
