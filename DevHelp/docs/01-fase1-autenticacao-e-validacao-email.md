# Fase 1 — Autenticação, Perfis e Validação de E-mail

## Objetivo
Implementar a base de autenticação do DevHelp com:
- Usuário customizado (`ApplicationUser`)
- Perfis `Aluno` e `Docente`
- Validação de domínios institucionais com `MailKit`/`MimeKit`
- Cadastro com seleção de perfil
- Criação automática de papéis no startup

## Regras de domínio implementadas

### Aluno
- `@aluno.senai.br`
- `@edu.senai.br`
- `@sp.senai.br`
- `@docente.senai.br`

### Docente
- `@sp.senai.br`
- `@docente.senai.br`
- `@edu.senai.br`

## Alterações realizadas

1. `DevHelp.csproj`
   - Adicionado pacote `MailKit` (inclui `MimeKit`).

2. `Program.cs`
   - Troca do Identity padrão para `ApplicationUser`.
   - Inclusão de `Roles`.
   - Registro da política de domínio e validador de usuário.
   - Seed de papéis padrão (`Aluno`, `Docente`).

3. `Data/ApplicationDbContext.cs`
   - Atualizado para `IdentityDbContext<ApplicationUser>`.

4. `Models/Identity/*`
   - `ApplicationUser` com campo `UserType`.
   - Enum `UserType` (`Unknown`, `Student`, `Teacher`).

5. `Services/Identity/*`
   - `IEmailDomainPolicy`.
   - `SenaiEmailDomainPolicy` com validação sintática por `MimeKit.MailboxAddress.TryParse` e validação de domínio.
   - `SenaiEmailUserValidator` integrado ao pipeline do Identity.

6. `Data/IdentityRoleSeeder.cs`
   - Seed automático de papéis no startup.

7. `Areas/Identity/Pages/Account/Register.*`
   - Página de cadastro customizada com seleção de perfil.
   - Validação de domínio por perfil.
   - Associação automática de papel (`Aluno`/`Docente`) após criação.

8. `Views/Shared/_LoginPartial.cshtml`
   - Ajustado para `ApplicationUser`.

## Próximos passos recomendados

1. Criar e aplicar migration para `UserType` no usuário:
   - `Add-Migration AddUserTypeToApplicationUser`
   - `Update-Database`

2. Configurar envio real de e-mail (`IEmailSender<ApplicationUser>`) para confirmação de conta.

3. Criar telas protegidas por papel:
   - Área de aluno
   - Área de docente

4. Iniciar módulo de categorias de atendimento (CRUD do docente).
