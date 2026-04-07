namespace MicCheck.Api.Identities;

public class IdentityTrait
{
    public int Id { get; init; }
    public int IdentityId { get; init; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public TraitValueType ValueType { get; set; }
}

public enum TraitValueType { String, Integer, Float, Boolean }
