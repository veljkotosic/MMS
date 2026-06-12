namespace MMS.Core.Filters;

public interface IFilterOptions
{
    string? Validate();

    void ValidateAndThrow()
    {
        var error = Validate();

        if (error != null)
        {
            throw new ArgumentOutOfRangeException(nameof(IFilterOptions), error);
        }
    }
}
