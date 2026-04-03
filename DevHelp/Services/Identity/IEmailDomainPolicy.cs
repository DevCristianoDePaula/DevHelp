using DevHelp.Models.Identity;

namespace DevHelp.Services.Identity
{
    // Contrato da política responsável por validar domínios de e-mail por tipo de usuário.
    public interface IEmailDomainPolicy
    {
        // Retorna verdadeiro quando o e-mail pertence aos domínios permitidos para o tipo informado.
        bool IsEmailAllowedForUserType(string email, UserType userType);
    }
}
