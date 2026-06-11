using System.Net.Mail;
using LanguageLearning.Domain;

namespace LanguageLearning.API.Common;

public static class ApiValidation
{
    public static bool IsEmail(string value)
    {
        try
        {
            return new MailAddress(value.Trim()).Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool IsRole(string value) =>
        value is Roles.Admin or Roles.Teacher or Roles.Student or Roles.Receptionist;
}
