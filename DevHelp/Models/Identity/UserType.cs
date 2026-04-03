namespace DevHelp.Models.Identity
{
    // Define os tipos de perfil suportados no sistema de HelpDesk.
    public enum UserType
    {
        // Valor padrão para cenários onde o tipo ainda não foi informado.
        Unknown = 0,
        // Perfil destinado aos alunos.
        Student = 1,
        // Perfil destinado aos docentes.
        Teacher = 2,
        // Perfil destinado ao administrador interno do DevHelp.
        Admin = 3
    }
}
