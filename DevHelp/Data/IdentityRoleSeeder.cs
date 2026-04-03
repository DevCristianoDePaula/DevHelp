using Microsoft.AspNetCore.Identity;
using DevHelp.Models.Identity;

namespace DevHelp.Data
{
    // Responsável por criar papéis padrão necessários para autorização do sistema.
    public static class IdentityRoleSeeder
    {
        // Lista fixa de papéis iniciais da aplicação.
        private static readonly string[] DefaultRoles = ["Aluno", "Professor", "Admin"];
        // Define e-mail padrão do administrador inicial do sistema.
        private const string AdminEmail = "admin@devhelp.com.br";
        // Define senha padrão do administrador inicial do sistema.
        private const string AdminPassword = "S3n41@790";

        // Garante que todos os papéis padrão existam no banco.
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            // Cria um escopo para resolver serviços com ciclo de vida correto.
            using var scope = serviceProvider.CreateScope();

            // Resolve o gerenciador de papéis do Identity.
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            // Resolve o gerenciador de usuários do Identity.
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Percorre todos os papéis definidos como padrão.
            foreach (var roleName in DefaultRoles)
            {
                // Verifica se o papel já existe para evitar duplicidade.
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                // Cria o papel quando ainda não está cadastrado.
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Busca o usuário administrador por e-mail.
            var adminUser = await userManager.FindByEmailAsync(AdminEmail);

            // Cria o usuário administrador quando ainda não existir.
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FullName = "Administrador DevHelp",
                    ClassGroup = "Administração",
                    UserType = UserType.Admin,
                    LockoutEnabled = true
                };

                // Persiste o novo usuário administrador com senha inicial.
                var createAdminResult = await userManager.CreateAsync(adminUser, AdminPassword);

                // Interrompe o startup quando não for possível criar o admin.
                if (!createAdminResult.Succeeded)
                {
                    throw new InvalidOperationException($"Falha ao criar usuário administrador padrão: {string.Join("; ", createAdminResult.Errors.Select(e => e.Description))}");
                }
            }

            // Garante que o usuário administrador esteja no papel Admin.
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
