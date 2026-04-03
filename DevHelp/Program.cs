using DevHelp.Data;
using DevHelp.Models.Identity;
using DevHelp.Services.Email;
using DevHelp.Services.Identity;
using DevHelp.Services.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("ServerSomee") ?? throw new InvalidOperationException("Connection string 'Server Somee' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Registra opções de SMTP para envio de e-mails pelo Gmail.
builder.Services.Configure<GmailSmtpOptions>(builder.Configuration.GetSection("EmailSettings"));
// Registra serviço de envio de e-mails do Identity com implementação SMTP real.
builder.Services.AddScoped<IEmailSender<ApplicationUser>, GmailEmailSender>();
// Registra serviço de e-mail transacional para processos assíncronos internos.
builder.Services.AddScoped<IAppEmailSender, GmailEmailSender>();
// Worker responsável por gerar relatórios PDF em segundo plano.
builder.Services.AddHostedService<AttendanceReportExportBackgroundService>();

// Registra a política de domínio para os e-mails institucionais do SENAI.
builder.Services.AddScoped<IEmailDomainPolicy, SenaiEmailDomainPolicy>();
// Registra o validador de usuário que utiliza MimeKit para analisar e validar e-mail/domínio.
builder.Services.AddScoped<IUserValidator<ApplicationUser>, SenaiEmailUserValidator>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        // Exige conta confirmada para permitir autenticação no sistema.
        options.SignIn.RequireConfirmedAccount = true;
    })
    // Habilita papéis para separar permissões de aluno e docente.
    .AddRoles<IdentityRole>()
    // Persiste dados do Identity no banco configurado da aplicação.
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Habilita autenticação por cookie do ASP.NET Identity.
app.UseAuthentication();
app.UseAuthorization();

// Redireciona usuários autenticados com e-mail confirmado para concluir perfil quando necessário.
app.Use(async (context, next) =>
{
    // Verifica se o usuário atual está autenticado.
    if (context.User.Identity?.IsAuthenticated == true)
    {
        // Captura a rota atual para evitar loop de redirecionamento.
        var path = context.Request.Path;

        // Define páginas liberadas mesmo com perfil incompleto.
        var isAllowedPath = path.StartsWithSegments("/Profile/Complete")
                            || path.StartsWithSegments("/Identity/Account/Logout")
                            || path.StartsWithSegments("/Identity/Account/Manage")
                            || path.StartsWithSegments("/Identity/Account/Login")
                            || path.StartsWithSegments("/Identity/Account/Register")
                            || path.StartsWithSegments("/Identity/Account/ConfirmEmail")
                            || path.StartsWithSegments("/Identity/Account/ResendEmailConfirmation");

        // Continua validação apenas quando a página atual não está na lista liberada.
        if (!isAllowedPath)
        {
            // Resolve o gerenciador de usuários para recuperar o usuário logado.
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            // Carrega os dados do usuário autenticado.
            var user = await userManager.GetUserAsync(context.User);

            // Redireciona para completar perfil quando necessário.
            if (user is not null && user.EmailConfirmed && !user.HasCompletedProfile())
            {
                context.Response.Redirect("/Profile/Complete");
                return;
            }
        }
    }

    // Continua o pipeline normalmente.
    await next();
});

app.MapStaticAssets();

// Define a rota raiz da aplicação para abrir a tela de login como página inicial.
app.MapGet("/", (HttpContext context) =>
{
    // Redireciona usuário autenticado para a home protegida.
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Home/Index");
        return Task.CompletedTask;
    }

    // Redireciona usuário não autenticado para a página de login.
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// Aplica migrações pendentes antes de qualquer operação de seed no Identity.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Garante que os papéis básicos existam antes do primeiro uso do sistema.
await IdentityRoleSeeder.SeedAsync(app.Services);

// Garante categorias padrão para organização inicial de chamados.
await CategorySeeder.SeedAsync(app.Services);

app.Run();
