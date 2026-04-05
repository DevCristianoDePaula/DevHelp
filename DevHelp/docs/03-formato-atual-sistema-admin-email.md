# Formato Atual do Sistema — DevHelp

## Visão geral atual
O DevHelp está estruturado com autenticação baseada em ASP.NET Identity, perfil complementar obrigatório e painel administrativo para gestão de usuários.
- O repositório inclui `README.md` principal com foco pedagógico (uso em sala de aula) e seção de galeria de telas para apresentação no GitHub.
- A seção de galeria no `README.md` foi atualizada com comparativo das principais telas em modo `Light` e `Dark`.

## Stack e arquitetura de interface
- Projeto ASP.NET Core com suporte a Controllers + Razor Pages.
- Fluxos de autenticação e conta via páginas de `Areas/Identity/Pages`.
- Layout visual moderno com Bootstrap 5.3 + Bootstrap Icons.
- Identidade visual da marca usando ícone voltado a desenvolvimento e mentoria (substituindo a lâmpada anterior).
- Design orientado a cards, espaçamentos limpos e efeitos sutis.
- Alternância de tema via botão flutuante com ícone (sol/lua) no canto inferior direito.
- Rodapé clean com visual alinhado ao tema da aplicação.
- Rodapé sem link de privacidade e conteúdo centralizado.
- Rodapé com altura reduzida para layout mais compacto.
- Confirmações de ações sensíveis usam modal padrão (`Sim/Não`) no layout, em vez de caixas nativas do navegador.

## Regras de acesso e identidade
- `ApplicationUser` é a entidade principal de usuário.
- Campos de perfil de negócio:
  - `UserType`
  - `FullName`
  - `ClassGroup`
- Todo novo cadastro entra automaticamente com:
  - `Role`: `Aluno`
  - `UserType`: `Student`
- Após e-mail confirmado, usuário é direcionado para completar perfil quando necessário.

## Política atual de domínios de e-mail
- Cadastro inicial (tela única com papel `Aluno`) aceita:
  - `@aluno.senai.br`
  - `@edu.senai.br`
  - `@sp.senai.br`
  - `@docente.senai.br`
- Perfil `Teacher` aceita:
  - `@sp.senai.br`
  - `@docente.senai.br`
  - `@edu.senai.br`
- Perfil `Admin` aceita:
  - `@devhelp.com.br`
- Domínio removido da política:
  - `@senaisp.edu.br`

## Papéis oficiais do sistema
- `Aluno`
- `Professor`
- `Admin`

## Seed administrativo padrão
No startup, o sistema garante:
1. Criação dos papéis oficiais.
2. Criação do usuário administrador padrão (caso não exista):
   - E-mail: `admin@devhelp.com.br`
   - Senha: `S3n41@790`
   - Papel: `Admin`

## Painel administrativo
Existe tela exclusiva para `Admin` em `Admin/Users` com:
- Listagem de usuários cadastrados.
- Busca por nome ou e-mail.
- Filtro por papel (`Aluno`, `Professor`, `Admin`).
- Paginação com tamanhos de página `10`, `20` e `50` (padrão `10`).
- Ordenação alfabética com `Admin` sempre no topo.
- Alteração de papel entre `Aluno` e `Professor`.
- Bloqueio e desbloqueio de usuário.
- Envio de link de redefinição de senha para o e-mail cadastrado.
- Acesso pelo menu superior em item exclusivo `Usuários`, posicionado ao lado de `Home`.

## Categorias (Admin)
- CRUD exclusivo para `Admin` em `Admin/Categories`.
- Funcionalidades implementadas:
  - listagem em tabela com visual padrão de cards;
  - criação de categoria;
  - edição de categoria;
  - exclusão de categoria com confirmação;
  - busca por nome/descrição;
  - paginação com `10`, `20` e `50` registros (padrão `10`).
- Ordenação alfabética por nome.
- Seed automático no startup com categorias padrão:
  - `Projetos`
  - `Frontend`
  - `Backend`
  - `Protótipo`
  - `Layout`
  - `Documentação`
  - `Mentoria`
  - `Suporte Infra`
  - `Outros`

## Navegação superior
- Menu possui destaque visual do item ativo.
- Para `Admin`, itens `Usuários` e `Categorias` ficam com indicação de seção atual.
- O item `Categorias` exibe badge com a quantidade total de categorias cadastradas.
- Perfis `Aluno`, `Professor` e `Admin` possuem atalho `Painel TV` no menu (abertura em nova aba).

## Envio de e-mail (Gmail SMTP)
O sistema usa implementação real de `IEmailSender<ApplicationUser>` com MailKit:
- Classe: `Services/Email/GmailEmailSender.cs`
- Opções: `Services/Email/GmailSmtpOptions.cs`
- Registro no `Program.cs`:
  - `Configure<GmailSmtpOptions>("EmailSettings")`
  - `AddScoped<IEmailSender<ApplicationUser>, GmailEmailSender>()`

### Padrão visual de e-mail
- E-mails de confirmação e redefinição usam HTML com layout moderno em card.
- Botão principal com destaque visual para ações (confirmar conta/redefinir senha).
- Templates também cobrem fluxos com código de confirmação/redefinição.

### Variáveis de configuração usadas (`appsettings.json`)
```json
"EmailSettings": {
  "FromName": "DevHelp",
  "FromEmail": "dev.cristianodepaula@gmail.com",
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "UserName": "dev.cristianodepaula@gmail.com",
  "Password": "houi oyiu ibft hiao"
}
```

## Fluxo principal resumido
1. Usuário se cadastra (entra como `Aluno`).
2. Recebe e-mail de confirmação com tela de feedback em card moderno.
3. Confirma conta e faz login.
4. Se perfil estiver incompleto, é redirecionado para `Profile/Complete`.
5. Admin gerencia usuários no painel dedicado.

## Perfil do usuário (tela única)
- A edição de perfil ocorre em `Profile/Complete`.
- O e-mail de cadastro é exibido somente leitura.
- A mesma tela permite alterar senha com:
  - senha atual
  - nova senha
  - confirmação da nova senha
- A validação de troca de senha só é acionada quando a senha atual é informada.
- Se a senha atual estiver vazia, o sistema entende que não há troca de senha e salva apenas os dados de perfil.
- O acesso ao perfil é feito clicando no avatar ou no nome do usuário no menu superior.

## Comportamento de navegação no login
- Usuário já autenticado que acessar `Identity/Account/Login` é redirecionado para `Home/Index`.
- Itens `Home` e `Privacidade` no menu superior só aparecem para usuário autenticado.
- O perfil `Aluno` não exibe atalho `Painel TV` no menu.

## Confirmação de e-mail (interface)
- `Account/RegisterConfirmation` segue padrão visual de card moderno.
- `Account/ConfirmEmail` segue o mesmo padrão, com:
  - botão para login
  - opção de reenviar confirmação
  - redirecionamento automático para login após 5 segundos apenas em caso de sucesso.
- `Account/ResendEmailConfirmation` também segue padrão visual de card moderno.

## Acesso negado (interface)
- `Identity/Account/AccessDenied` foi customizada no padrão visual moderno do projeto.
- Página utiliza card com mensagem orientativa e ações rápidas para:
  - voltar para `Home/Index`;
  - ir para `Identity/Account/Login`.

## Redefinição de senha (interface)
- `Account/ResetPassword` foi customizada no padrão visual moderno do projeto.
- `Account/ResetPasswordConfirmation` também segue o padrão de card e ação para login.

## Privacidade (interface)
- `Home/Privacy` foi reformulada para o padrão visual moderno do projeto.
- A tela usa card principal com seções objetivas sobre:
  - dados coletados;
  - finalidade de uso;
  - proteção dos dados;
  - canal de contato acadêmico.

## Chamados do aluno (abertura e fila)
- Implementado fluxo de abertura em `Tickets/Create` exclusivo para `Aluno`.
- Número do chamado é gerado automaticamente no padrão `AAAA-MM-000001` com sequência mensal.
- Campos da abertura:
  - categoria obrigatória;
  - prioridade obrigatória (`Baixa`, `Média`, `Alta`, `Urgente`);
  - professor preferencial opcional;
  - descrição obrigatória;
  - anexos de arquivo opcionais;
  - links opcionais (um por linha).
- Data/hora de abertura é registrada em UTC no momento da criação.
- Aluno acompanha seus chamados em `Tickets/My`.
- `Professor` e `Admin` acessam a fila em `Tickets/Queue`.
- Métrica da fila combina urgência e tempo de espera com score:
  - `score = minutos_em_espera + (prioridade * 240)`
  - ordenação por maior score, evitando que chamados antigos fiquem indefinidamente para trás.

## Chamados (detalhes, status e comentários)
- Implementada tela de detalhe em `Tickets/Details/{id}` para aluno dono, professor e admin.
- No detalhe são exibidos:
  - metadados do chamado (categoria, prioridade, status, abertura);
  - descrição original;
  - anexos e links;
  - histórico de comentários.
- Professores e administradores podem atualizar status para:
  - `Aberto`
  - `Em atendimento`
  - `Resolvido`
  - `Fechado`
  - `Cancelado`
- Todos os perfis autorizados no chamado (dono, professor, admin) podem comentar.
- A fila (`Tickets/Queue`) passou a listar apenas chamados pendentes (`Aberto` e `Em atendimento`).

## Chamados (SLA e alerta de atraso)
- Cada chamado recebe automaticamente um prazo de primeira resposta (`ResponseDueAtUtc`) na abertura.
- Janela de SLA por prioridade:
  - `Urgente`: 2 horas
  - `Alta`: 8 horas
  - `Média`: 24 horas
  - `Baixa`: 72 horas
- `Tickets/My` exibe o prazo SLA de cada chamado do aluno.
- `Tickets/Queue` exibe prazo e atraso em minutos com destaque visual para chamados vencidos.
- `Tickets/Details` exibe prazo SLA e alerta quando o chamado pendente está fora do prazo.

## Atendimento rápido para Professor/Admin
- Menu superior inclui ação `Iniciar atendimento` para `Professor` e `Admin`.
- Ao clicar, o sistema seleciona automaticamente o primeiro chamado da fila (mesma métrica de ordenação da fila).
- O comando `Iniciar atendimento` considera apenas chamados com status `Aberto` (não recaptura chamados já `Em atendimento`).
- Se o chamado estiver `Aberto`, o status muda automaticamente para `Em atendimento`.
- Em seguida, o sistema abre `Tickets/Details` do chamado e exibe uma modal de anúncio com:
  - número do próximo ticket;
  - nome do aluno.
- A modal dispara um som curto de chamada e também oferece botão `Tocar som` para repetir manualmente.
- Comportamento inspirado em chamada de fila (hospital/fast food), facilitando início imediato do atendimento.

## Fura-fila e devolução para fila
- Em `Tickets/Queue`, cada linha possui botão `Atender agora` para professor/admin iniciar atendimento direto de um ticket específico (fura-fila controlado).
- O atendimento direto também abre `Tickets/Details` com modal de chamada (ticket + aluno).
- Na tela de detalhes, professor/admin pode usar `Devolver para fila` quando o atendimento não foi resolvido.
- `Devolver para fila` ajusta status para `Aberto` e recoloca o chamado na ordenação normal da fila.

## Cancelamento de ticket
- Adicionado status `Cancelado` no fluxo de chamados.
- Tanto `Aluno` (na listagem `Tickets/My`) quanto `Professor/Admin` (na listagem `Tickets/Queue`) podem cancelar chamados `Aberto` ou `Em atendimento`.
- Chamados `Cancelado` não aparecem mais na fila operacional de atendimento.
- Regra adicional: chamado `Fechado` não pode ser alterado para `Cancelado`.
- O histórico de atendimentos (`Tickets/History`) inclui também chamados `Cancelado`.

## Histórico de atendimentos (Professor/Admin)
- Menu de `Professor` e `Admin` inclui item `Histórico de atendimentos`.
- Tela `Tickets/History` exibe listagem geral de atendimentos finalizados.
- Busca textual por:
  - aluno;
  - e-mail (aluno/docente);
  - docente que atendeu;
  - número do ticket.
- Filtros disponíveis:
  - categoria;
  - prioridade.
- Filtro adicional disponível:
  - range de datas de finalização (`Finalizado em (de)` / `Finalizado em (até)`).
- Paginação no mesmo padrão da listagem de usuários (`10`, `20`, `50`).
- A tela permite solicitar exportação da listagem para PDF.
- A exportação de PDF roda em segundo plano com status de processamento.
- Quando o PDF fica pronto, o professor recebe notificação por toast no canto inferior direito com link para abrir.
- O relatório final também é enviado por e-mail para o professor solicitante como anexo PDF.

## Professor preferencial no chamado
- Na abertura (`Tickets/Create`), o aluno pode escolher um professor específico de forma opcional.
- Se houver professor preferencial definido:
  - apenas esse professor (ou admin) pode iniciar atendimento do chamado;
  - na fila, outros professores visualizam o chamado como `Reservado`;
  - ao usar `Iniciar atendimento`, se o próximo item estiver reservado para outro professor, a vez passa para o próximo chamado elegível.
- Se não houver professor preferencial, o chamado permanece livre para qualquer professor respeitando a ordem da fila.
- A tela `Tickets/Details` agora exibe explicitamente o campo `Professor preferencial`.

## Dashboard inicial operacional
- `Home/Index` foi convertido para dashboard moderno orientado a atendimento.
- Exibe métricas de topo:
  - total de professores;
  - atendimentos em curso;
  - total pendente na fila.
- Card `Professores e atendimento atual` mostra professor, ticket em atendimento e horário de início.
- Card `Próximos da fila` mostra ordem operacional dos próximos chamados.
- Card `Últimos atendimentos finalizados` mostra:
  - professor;
  - ticket;
  - data/hora do fim;
  - tempo total de atendimento em minutos.
- Cards do dashboard podem ser reordenados por drag and drop, com persistência no navegador via `localStorage`.
- O dashboard passou a incluir gráficos variados para leitura rápida:
  - donut de distribuição por categoria;
  - polar area de prioridades pendentes;
  - barras de indicadores de SLA;
  - linha de aberturas x finalizações nos últimos 10 dias;
  - barras horizontais de finalizações por professor.
- No login como `Aluno`, o botão `Iniciar próximo atendimento` não é exibido.

## Painel público para TV
- Criada rota pública `GET /painel-tv` para exibição do dashboard em monitor/TV sem necessidade de login.
- Criado endpoint público `GET /painel-tv/ultima-chamada` para consulta da última chamada de ticket.
- Ao iniciar atendimento (`Iniciar atendimento` ou `Atender agora`), a aplicação registra anúncio em `TicketCallAnnouncements`.
- A modal de chamada (ticket + aluno + som) foi movida da tela do professor para o painel público da TV.
- O painel TV faz polling periódico da última chamada e exibe modal automaticamente quando há novo anúncio.
- A modal do painel TV fecha automaticamente após 5 segundos.
- O layout visual do painel TV foi alinhado ao dashboard principal (mesmo estilo de cards/tabelas/gráficos), sem botão de iniciar atendimento.
- No painel TV, o topo exibe apenas logo e botão `Sair` quando autenticado, sem menus e sem identificação do usuário logado.
- O painel TV usa largura ampliada (`container-fluid`) com ajustes de espaçamento/altura para caber mais conteúdo sem scroll vertical.
- O painel TV mantém margem lateral equivalente a `mx-5` (respiro visual nas laterais).
- Correção de responsividade aplicada: o respiro lateral do painel TV é feito com padding interno (não margem externa), evitando estouro de conteúdo no lado direito.
- No `painel-tv`, o header global foi removido para maximizar área útil da tela.
- O logo `DevHelp` é exibido na mesma linha do título `Dashboard de Atendimentos`, no lado oposto.
- A modal de chamada no painel TV é totalmente automática e sem botões de ação:
  - toca som ao abrir;
  - repete o som após 2 segundos;
  - repete novamente após mais 2 segundos (3º toque);
  - fecha automaticamente 1 segundo após o 3º toque.
- Ajuste de confiabilidade do áudio no painel TV:
  - reutiliza `AudioContext` para evitar bloqueios intermitentes;
  - tenta `resume()` quando o contexto estiver suspenso;
  - possui fallback por voz (`SpeechSynthesis`) quando o navegador bloquear o beep.
- Painel TV inclui também os dois cards adicionais do dashboard principal:
  - `Prioridades pendentes`;
  - `Aberturas x finalizações (10 dias)`.
- O menu superior agora inclui atalho `Painel TV` para perfis `Aluno` e `Professor` (abertura em nova aba).

## Rastreio de atendimento por professor
- O ticket passou a armazenar:
  - `AssignedProfessorId` (professor responsável);
  - `ServiceStartedAtUtc` (início do atendimento);
  - `ServiceFinishedAtUtc` (fim do atendimento).
- Ao iniciar atendimento (`Iniciar atendimento` ou `Atender agora`), o sistema grava professor e início.
- Ao finalizar (`Resolvido/Fechado`), grava horário de fim.
- Ao devolver para fila, limpa vínculo de professor e tempos do atendimento atual.
