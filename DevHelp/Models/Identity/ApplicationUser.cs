using Microsoft.AspNetCore.Identity;

namespace DevHelp.Models.Identity
{
    // Representa o usuário autenticável do sistema com informações específicas do domínio.
    public class ApplicationUser : IdentityUser
    {
        // Armazena o tipo de perfil escolhido no cadastro (Aluno ou Docente).
        public UserType UserType { get; set; }

        // Armazena o nome completo informado na etapa de conclusão do perfil.
        public string? FullName { get; set; }

        // Armazena a turma do usuário (campo obrigatório para aluno e docente neste fluxo).
        public string? ClassGroup { get; set; }
    }
}
