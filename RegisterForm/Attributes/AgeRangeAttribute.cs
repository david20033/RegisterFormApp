using System;
using System.ComponentModel.DataAnnotations;

public class AgeRangeAttribute : ValidationAttribute
{
    private readonly int _minAge;
    private readonly int _maxAge;

    public AgeRangeAttribute(int minAge, int maxAge)
    {
        _minAge = minAge;
        _maxAge = maxAge;
        ErrorMessage = $"Age must be between {_minAge} and {_maxAge} years.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (value is DateTime date)
        {
            var age = DateTime.Today.Year - date.Year;
            if (date.Date > DateTime.Today.AddYears(-age)) age--;

            if (age < _minAge || age > _maxAge)
                return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
