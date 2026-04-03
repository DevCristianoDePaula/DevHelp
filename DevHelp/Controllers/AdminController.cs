using System.Text;
using DevHelp.Data;
using DevHelp.Models.Admin;
using DevHelp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace DevHelp.Controllers
{
    // Restringe o acesso do painel administrativo somente para usuários com papel Admin.
    [Authorize(Roles = "Admin")]
    public class AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmailSender<ApplicationUser> emailSender,
        ApplicationDbContext dbContext) : Controller
    {
        // Gerencia operações de usuários cadastrados no Identity.
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        // Gerencia criação e verificação de papéis.
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        // Envia links de redefinição de senha por e-mail.
        private readonly IEmailSender<ApplicationUser> _emailSender = emailSender;
        // Acessa dados de categorias administrativas no banco.
        private readonly ApplicationDbContext _dbContext = dbContext;

        // Exibe a listagem de usuários para administração com filtros e paginação.
        public async Task<IActionResult> Users(string? searchTerm = null, string? roleFilter = null, int page = 1, int pageSize = 10)
        {
            // Normaliza tamanho de página para opções suportadas na interface.
            var allowedPageSizes = new[] { 10, 20, 50 };
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            // Garante número de página válido para evitar paginação inconsistente.
            if (page < 1)
            {
                page = 1;
            }

            // Carrega todos os usuários cadastrados para exibição no painel.
            var users = _userManager.Users.ToList();
            // Inicializa coleção para a view com dados consolidados de usuário e papel.
            var model = new List<UserManagementViewModel>(users.Count);

            // Percorre usuários para resolver papéis e status de bloqueio.
            foreach (var user in users)
            {
                // Obtém os papéis associados ao usuário atual.
                var roles = await _userManager.GetRolesAsync(user);
                // Seleciona papel principal de negócio mantendo Admin quando aplicável.
                var primaryRole = roles.Contains("Admin") ? "Admin" : roles.FirstOrDefault(r => r is "Professor" or "Aluno") ?? "Aluno";

                // Monta a linha de exibição da grid administrativa.
                model.Add(new UserManagementViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? "(sem e-mail)",
                    DisplayName = user.FullName ?? user.UserName ?? "Usuário",
                    Role = primaryRole,
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            // Aplica busca textual por nome ou e-mail quando informada.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearch = searchTerm.Trim();
                model = model
                    .Where(u => u.DisplayName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                             || u.Email.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Aplica filtro de papel quando houver seleção na interface.
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                model = model
                    .Where(u => string.Equals(u.Role, roleFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Ordena alfabética com regra de negócio que mantém Admin no topo.
            model = model
                .OrderBy(u => u.Role == "Admin" ? 0 : 1)
                .ThenBy(u => u.DisplayName)
                .ThenBy(u => u.Email)
                .ToList();

            // Calcula totais para paginação.
            var totalRecords = model.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));

            // Ajusta página atual quando ultrapassar limite após filtros.
            if (page > totalPages)
            {
                page = totalPages;
            }

            // Recorta apenas o subconjunto da página atual.
            var pagedUsers = model
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Monta ViewModel da página com lista e metadados de busca/paginação.
            var pageModel = new UserManagementPageViewModel
            {
                Users = pagedUsers,
                SearchTerm = searchTerm?.Trim() ?? string.Empty,
                RoleFilter = roleFilter?.Trim() ?? string.Empty,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords
            };

            // Renderiza a página de usuários com dados prontos para ação.
            return View(pageModel);
        }

        // Permite alterar o papel de um usuário entre Aluno e Professor.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string targetRole)
        {
            // Valida entrada de papel para evitar valores fora da regra.
            if (targetRole is not ("Aluno" or "Professor"))
            {
                TempData["AdminError"] = "Papel inválido. Selecione Aluno ou Professor.";
                return RedirectToAction(nameof(Users));
            }

            // Busca usuário alvo pelo identificador informado.
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["AdminError"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Users));
            }

            // Impede alteração de papel para contas administrativas.
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["AdminError"] = "Não é permitido alterar o papel de contas administradoras.";
                return RedirectToAction(nameof(Users));
            }

            // Garante que o papel de destino exista no banco.
            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(targetRole));
            }

            // Carrega papéis atuais para remoção controlada antes da troca.
            var currentRoles = await _userManager.GetRolesAsync(user);
            // Remove papéis de negócio para aplicar somente o novo papel selecionado.
            var removableRoles = currentRoles.Where(r => r is "Aluno" or "Professor" or "Docente").ToList();
            if (removableRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, removableRoles);
            }

            // Adiciona o novo papel selecionado pelo administrador.
            await _userManager.AddToRoleAsync(user, targetRole);

            // Mantém UserType sincronizado com o papel escolhido no painel.
            user.UserType = targetRole == "Professor" ? UserType.Teacher : UserType.Student;
            await _userManager.UpdateAsync(user);

            TempData["AdminSuccess"] = $"Papel do usuário {user.Email} atualizado para {targetRole}.";
            return RedirectToAction(nameof(Users));
        }

        // Bloqueia um usuário até data futura para impedir login.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string userId)
        {
            // Busca usuário alvo para bloqueio.
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["AdminError"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Users));
            }

            // Impede bloqueio de conta administrativa.
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["AdminError"] = "Não é permitido bloquear contas administradoras.";
                return RedirectToAction(nameof(Users));
            }

            // Ativa lockout e define data longa para bloqueio efetivo.
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            TempData["AdminSuccess"] = $"Usuário {user.Email} bloqueado com sucesso.";
            return RedirectToAction(nameof(Users));
        }

        // Remove bloqueio de um usuário para permitir novo login.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            // Busca usuário alvo para desbloqueio.
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["AdminError"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(Users));
            }

            // Libera lockout removendo data de bloqueio.
            await _userManager.SetLockoutEndDateAsync(user, null);

            TempData["AdminSuccess"] = $"Usuário {user.Email} desbloqueado com sucesso.";
            return RedirectToAction(nameof(Users));
        }

        // Envia e-mail com link para redefinição de senha do usuário.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswordReset(string userId)
        {
            // Busca usuário para geração de token de redefinição.
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["AdminError"] = "Usuário inválido para envio de redefinição de senha.";
                return RedirectToAction(nameof(Users));
            }

            // Gera token de redefinição e converte para URL segura.
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // Monta callback para página padrão de reset do Identity.
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code, email = user.Email },
                protocol: Request.Scheme);

            // Envia e-mail com link de redefinição de senha.
            await _emailSender.SendPasswordResetLinkAsync(user, user.Email, callbackUrl!);

            TempData["AdminSuccess"] = $"Link de redefinição enviado para {user.Email}.";
            return RedirectToAction(nameof(Users));
        }

        // Exibe listagem paginada de categorias com busca por nome/descrição.
        public async Task<IActionResult> Categories(string? searchTerm = null, int page = 1, int pageSize = 10)
        {
            // Normaliza tamanho da página para opções permitidas na interface.
            var allowedPageSizes = new[] { 10, 20, 50 };
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            // Garante página mínima válida.
            if (page < 1)
            {
                page = 1;
            }

            // Inicia consulta base de categorias sem rastreamento para listagem.
            var query = _dbContext.Categories.AsNoTracking().AsQueryable();

            // Aplica filtro textual quando termo de busca for informado.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearch = searchTerm.Trim();
                query = query.Where(c => c.Name.Contains(normalizedSearch)
                                      || (c.Description != null && c.Description.Contains(normalizedSearch)));
            }

            // Ordena alfabeticamente por nome da categoria.
            query = query.OrderBy(c => c.Name);

            // Calcula totais para paginação.
            var totalRecords = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));

            // Ajusta página quando valor informado estiver acima do limite.
            if (page > totalPages)
            {
                page = totalPages;
            }

            // Carrega somente o recorte de dados da página atual.
            var categories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Monta dados para renderização da tela.
            var pageModel = new CategoryManagementPageViewModel
            {
                Categories = categories,
                SearchTerm = searchTerm?.Trim() ?? string.Empty,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords
            };

            return View(pageModel);
        }

        // Exibe formulário para cadastrar nova categoria.
        public IActionResult CreateCategory()
        {
            return View(new CategoryFormViewModel());
        }

        // Persiste nova categoria informada pelo administrador.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryFormViewModel input)
        {
            // Mantém formulário quando houver erro de validação.
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            // Normaliza nome para gravação e verificação de duplicidade.
            var normalizedName = input.Name.Trim();

            // Impede criação de categorias com o mesmo nome.
            var categoryExists = await _dbContext.Categories
                .AnyAsync(c => c.Name.ToLower() == normalizedName.ToLower());
            if (categoryExists)
            {
                ModelState.AddModelError(nameof(input.Name), "Já existe uma categoria com este nome.");
                return View(input);
            }

            // Cria e persiste nova categoria.
            var category = new Category
            {
                Name = normalizedName,
                Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            TempData["AdminSuccess"] = "Categoria cadastrada com sucesso.";
            return RedirectToAction(nameof(Categories));
        }

        // Exibe formulário para edição de categoria existente.
        public async Task<IActionResult> EditCategory(int id)
        {
            // Busca categoria a ser editada.
            var category = await _dbContext.Categories.FindAsync(id);
            if (category is null)
            {
                TempData["AdminError"] = "Categoria não encontrada.";
                return RedirectToAction(nameof(Categories));
            }

            // Mapeia entidade para formulário de edição.
            var model = new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return View(model);
        }

        // Persiste alterações da categoria selecionada.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryFormViewModel input)
        {
            // Mantém formulário quando houver erro de validação.
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            // Busca categoria para aplicar alterações.
            var category = await _dbContext.Categories.FindAsync(input.Id);
            if (category is null)
            {
                TempData["AdminError"] = "Categoria não encontrada.";
                return RedirectToAction(nameof(Categories));
            }

            // Normaliza nome para comparação e persistência.
            var normalizedName = input.Name.Trim();

            // Impede conflito de nome com outra categoria existente.
            var nameInUse = await _dbContext.Categories
                .AnyAsync(c => c.Id != input.Id && c.Name.ToLower() == normalizedName.ToLower());
            if (nameInUse)
            {
                ModelState.AddModelError(nameof(input.Name), "Já existe uma categoria com este nome.");
                return View(input);
            }

            // Atualiza dados de domínio da categoria.
            category.Name = normalizedName;
            category.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            category.UpdatedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            TempData["AdminSuccess"] = "Categoria atualizada com sucesso.";
            return RedirectToAction(nameof(Categories));
        }

        // Exclui uma categoria do sistema.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // Busca categoria a ser removida.
            var category = await _dbContext.Categories.FindAsync(id);
            if (category is null)
            {
                TempData["AdminError"] = "Categoria não encontrada.";
                return RedirectToAction(nameof(Categories));
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();

            TempData["AdminSuccess"] = "Categoria excluída com sucesso.";
            return RedirectToAction(nameof(Categories));
        }
    }
}
