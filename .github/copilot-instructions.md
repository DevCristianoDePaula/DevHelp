# Copilot Instructions

## General Guidelines
- Prefere documentação dos processos em arquivos Markdown e código comentado de forma detalhada, idealmente linha a linha.
- Mantenha documentação contínua do estado atual do sistema DevHelp em arquivos Markdown e contextualize-se sempre por esse formato antes de continuar mudanças.
- Continue com ajustes iterativos no projeto, mantendo a evolução contínua e incremental das funcionalidades já implementadas no DevHelp.

## Project-Specific Rules
- Retome o desenvolvimento do DevHelp mantendo as preferências já definidas: dashboard inicial moderno com todos os cards em drag and drop, gráficos de categoria/prioridade/SLA/volumes, e evolução contínua do fluxo de tickets com documentação em Markdown atualizada a cada etapa aprovada.
- Quando uma solução for aprovada, implemente diretamente e registre um resumo da decisão para facilitar as próximas implementações no DevHelp. Continue implementando diretamente as próximas etapas e atualizando a memória de decisões durante a evolução do fluxo de tickets no DevHelp.
- O cadastro no DevHelp permanece em tela única com papel inicial Aluno, deve aceitar domínios docentes (@sp.senai.br e @docente.senai.br), não deve permitir @senaisp.edu.br, e páginas de confirmação devem seguir o visual moderno com card no padrão da Home.
- Os e-mails do Identity devem usar HTML moderno com card e botão destacado; ConfirmEmail, RegisterConfirmation, ResendEmailConfirmation e ResetPassword devem seguir o mesmo design com opção de ir ao login e redirecionamento automático em 5s, que deve ocorrer somente em caso de sucesso; em falha, manter o usuário na página com orientação para reenviar confirmação.
- A tela Profile/Complete deve ser única para editar perfil e trocar senha (senha atual + nova + confirmação), com e-mail somente leitura e sem acesso separado a Manage/Index. A troca de senha só deve validar quando a senha atual for preenchida; se a senha atual estiver vazia, salvar apenas perfil sem exigir nova/confirmação, com mensagens em pt-BR e campos de senha iniciando vazios.
- Usuário autenticado não deve permanecer na tela de login (deve ir para Home).
- O item Meu Perfil deve sair do menu e o acesso ao perfil deve ser pelo avatar ou nome do usuário.
- Implementar o lado do aluno para abertura de tickets com ID sequencial no padrão AAAA-MM-XXXXX1, informando categoria, prioridade, descrição, anexos (anexo não é obrigatório) e links; ao salvar deve registrar data/hora de abertura e entrar em fila com prioridade por urgência sem deixar itens antigos para trás (métrica prioridade + tempo). O aluno pode escolher um professor preferencial opcional; nesse caso, apenas esse professor pode chamar o ticket, e outros professores devem pular para o próximo da fila automaticamente.
- Professores devem poder furar a fila chamando diretamente um ticket específico da lista e também devolver para a fila um ticket não resolvido.
- No painel TV, manter margem lateral equivalente a `mx-5` para dar respiro visual nas laterais.
- Ao usar 'Iniciar atendimento' (próximo da fila), devem ser considerados apenas chamados com status Aberto, nunca chamados já Em atendimento.
- Confirmações de ação devem usar modal padrão com opções Sim/Não no layout, evitando caixas nativas de navegador (`confirm`/`alert`) para manter consistência visual.

## Theme
- A alternância dark/light deve usar botão flutuante moderno com ícone sol/lua no canto inferior direito (não no menu), e o tema deve persistir entre views/actions, mudando somente por clique do usuário. Os itens Home/Privacidade do menu devem ficar ocultos para usuários não autenticados.

## Email Settings
- Utilize Gmail SMTP com configuração via EmailSettings.

## Public Panel
- No painel público da TV, a modal de próximo atendimento deve ser totalmente automática: sem botões, tocar som 3 vezes (imediato, +2s, +4s) e fechar 1s após o terceiro toque (+5s).