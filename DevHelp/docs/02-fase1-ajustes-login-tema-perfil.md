# Fase 1 (Ajustes) — Login Inicial, Tema e Perfil Complementar

## Objetivo
Aplicar os ajustes funcionais solicitados antes da próxima fase:
- Página inicial deve abrir em `Login`
- Uso de `Bootstrap 5.3`
- Tema `dark` como padrão e `light` opcional com botão de alternância
- Cadastro e login somente com e-mail/senha (sem login externo)
- Após confirmação de e-mail, usuário deve completar perfil
- Usuário deve conseguir alterar o próprio perfil
- Avatar textual com iniciais (primeira letra do primeiro e último nome)

## O que foi implementado

## 1) Login como página inicial
- A rota raiz `/` redireciona para `/Identity/Account/Login` quando usuário não autenticado.
- Usuário autenticado é enviado para `/Home/Index`.

## 2) Bootstrap 5.3
- Layout atualizado para CDN oficial `Bootstrap 5.3.3` (CSS e JS bundle).

## 3) Tema dark/light
- Tema padrão definido como `dark`.
- Botão `Tema` adicionado na barra de navegação.
- Alternância persistida via `localStorage`.

## 4) Login apenas por e-mail e senha
- Página `Login` do Identity foi customizada para fluxo de e-mail/senha.
- Nenhuma ação de login externo foi incluída na interface.

## 5) Perfil complementar após confirmação de e-mail
- `ApplicationUser` recebeu campos:
  - `FullName`
  - `ClassGroup`
- Middleware verifica usuário autenticado com e-mail confirmado:
  - Se perfil incompleto, redireciona para `/Profile/Complete`.

## 6) Edição de perfil
- Criada página Razor `Pages/Profile/Complete`.
- Permite preencher/editar `Nome completo` e `Turma`.

## 7) Avatar textual por iniciais
- No menu de usuário, avatar é exibido com iniciais geradas do nome completo.
- Regra: primeira letra do primeiro nome + primeira letra do último nome.

## Arquivos alterados/adicionados

- `Program.cs`
- `Models/Identity/ApplicationUser.cs`
- `Models/Identity/ApplicationUserExtensions.cs`
- `Areas/Identity/Pages/Account/Login.cshtml`
- `Areas/Identity/Pages/Account/Login.cshtml.cs`
- `Pages/_ViewImports.cshtml`
- `Pages/Profile/Complete.cshtml`
- `Pages/Profile/Complete.cshtml.cs`
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_LoginPartial.cshtml`
- `wwwroot/js/site.js`
- `wwwroot/css/site.css`
- `Views/Home/Index.cshtml`
- `Views/Home/Privacy.cshtml`

## Pendências para próxima fase

1. Criar migration para os novos campos de usuário:
   - `Add-Migration AddProfileFieldsToApplicationUser`
   - `Update-Database`

2. Implementar módulo de chamados e anexos.
   - Regra já definida para próximos passos: aceitar somente `imagem` e `PDF`.
