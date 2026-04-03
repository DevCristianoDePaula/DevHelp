using DevHelp.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace DevHelp.Data
{
    // Responsável por garantir categorias padrão do sistema na inicialização.
    public static class CategorySeeder
    {
        // Lista inicial de categorias administrativas do DevHelp.
        private static readonly string[] DefaultCategories =
        [
            "Projetos",
            "Frontend",
            "Backend",
            "Protótipo",
            "Layout",
            "Documentação",
            "Mentoria",
            "Suporte Infra",
            "Outros"
        ];

        // Garante que as categorias padrão existam no banco sem duplicidade.
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            // Cria escopo de serviços para resolver o DbContext.
            using var scope = serviceProvider.CreateScope();

            // Resolve contexto de dados da aplicação.
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Carrega nomes já existentes para evitar inclusão duplicada.
            var existingNames = await dbContext.Categories
                .Select(c => c.Name.ToLower())
                .ToListAsync();

            var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

            // Adiciona apenas categorias que ainda não existem.
            foreach (var categoryName in DefaultCategories)
            {
                if (existingSet.Contains(categoryName))
                {
                    continue;
                }

                dbContext.Categories.Add(new Category
                {
                    Name = categoryName,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            // Persiste alterações somente quando houver novas categorias.
            if (dbContext.ChangeTracker.HasChanges())
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
