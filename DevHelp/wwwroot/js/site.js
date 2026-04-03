// Controla o tema visual padrão da aplicação (dark por padrão, light opcional).
(() => {
    // Define chave de armazenamento local do tema selecionado.
    const themeStorageKey = "devhelp-theme";
    // Captura o elemento raiz para alterar atributo de tema do Bootstrap.
    const rootElement = document.documentElement;
    // Captura botão de alternância de tema.
    const themeToggleButton = document.getElementById("theme-toggle");
    // Captura ícone do botão flutuante para indicar tema atual.
    const themeToggleIcon = document.getElementById("theme-toggle-icon");

    // Atualiza o tema ativo e o ícone do botão flutuante.
    const applyTheme = (theme) => {
        // Define o tema no atributo reconhecido pelo Bootstrap 5.3.
        rootElement.setAttribute("data-bs-theme", theme);

        // Atualiza ícone para indicar ação intuitiva de troca do tema.
        if (themeToggleIcon) {
            themeToggleIcon.className = theme === "dark" ? "bi bi-sun-fill" : "bi bi-moon-stars-fill";
        }
    };

    // Lê o tema salvo; quando ausente, mantém dark como padrão.
    const savedTheme = localStorage.getItem(themeStorageKey) || "dark";
    applyTheme(savedTheme);

    // Alterna entre dark e light ao clicar no botão.
    themeToggleButton?.addEventListener("click", () => {
        // Obtém tema atual definido no documento.
        const currentTheme = rootElement.getAttribute("data-bs-theme") || "dark";
        // Calcula o próximo tema.
        const nextTheme = currentTheme === "dark" ? "light" : "dark";
        // Salva escolha do usuário no navegador.
        localStorage.setItem(themeStorageKey, nextTheme);
        // Aplica mudança visual imediatamente.
        applyTheme(nextTheme);
    });
})();

// Substitui confirmações nativas por modal padrão com ações Sim/Não.
(() => {
    const modalElement = document.getElementById("confirmActionModal");
    const messageElement = document.getElementById("confirmActionModalMessage");
    const yesButton = document.getElementById("confirmActionModalYes");

    if (!modalElement || !messageElement || !yesButton || typeof bootstrap === "undefined") {
        return;
    }

    const confirmModal = new bootstrap.Modal(modalElement);
    let pendingForm = null;

    document.addEventListener("click", (event) => {
        const clickedButton = event.target.closest('[data-confirm-submit="true"]');
        if (!clickedButton) {
            return;
        }

        const parentForm = clickedButton.closest("form");
        if (!parentForm) {
            return;
        }

        event.preventDefault();
        pendingForm = parentForm;

        const customMessage = clickedButton.getAttribute("data-confirm-message");
        messageElement.textContent = customMessage && customMessage.trim().length > 0
            ? customMessage
            : "Tem certeza que deseja continuar?";

        confirmModal.show();
    });

    yesButton.addEventListener("click", () => {
        if (!pendingForm) {
            return;
        }

        const formToSubmit = pendingForm;
        pendingForm = null;
        confirmModal.hide();
        formToSubmit.submit();
    });

    modalElement.addEventListener("hidden.bs.modal", () => {
        pendingForm = null;
    });
})();
