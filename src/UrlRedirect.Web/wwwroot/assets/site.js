(function () {
    const form = document.getElementById("redirect-form");

    if (!form) {
        return;
    }

    const aliasInput = document.getElementById("alias");
    const targetUrlInput = document.getElementById("target-url");
    const aliasError = document.getElementById("alias-error");
    const targetUrlError = document.getElementById("target-url-error");
    const validationSummary = document.getElementById("validation-summary");
    const resultCard = document.getElementById("result-card");
    const resultUrl = document.getElementById("result-url");
    const copyResultButton = document.getElementById("copy-result-button");
    const showQrButton = document.getElementById("show-qr-button");
    const qrModal = document.getElementById("qr-modal");
    const qrCodeImage = document.getElementById("qr-code-image");
    const qrModalLink = document.getElementById("qr-modal-link");
    const closeQrButton = document.getElementById("close-qr-button");
    const submitButton = document.getElementById("submit-button");

    const RESERVED_ALIASES = new Set(["api", "ui", "admin", "login", "logout"]);
    const ALIAS_PATTERN = /^[a-z0-9][a-z0-9\-_]{2,39}$/;

    function validateAliasValue(value) {
        if (RESERVED_ALIASES.has(value)) {
            return `"${value}" is a reserved alias and cannot be used.`;
        }
        if (!/^[a-z0-9\-_]*$/.test(value)) {
            return "Only lowercase letters, digits, hyphens, and underscores are allowed.";
        }
        if (value.length > 40) {
            return "Alias must be 40 characters or fewer.";
        }
        if (!ALIAS_PATTERN.test(value)) {
            return "Must start with a letter or digit and be 3–40 characters.";
        }
        return null;
    }

    aliasInput.addEventListener("input", function () {
        const value = aliasInput.value.trim().toLowerCase();

        if (value.length < 3) {
            aliasError.textContent = "";
            aliasInput.classList.remove("is-error");
            return;
        }

        const error = validateAliasValue(value);
        if (error) {
            aliasError.textContent = error;
            aliasInput.classList.add("is-error");
        } else {
            aliasError.textContent = "";
            aliasInput.classList.remove("is-error");
        }
    });

    function clearFeedback() {
        aliasError.textContent = "";
        targetUrlError.textContent = "";
        aliasInput.classList.remove("is-error");
        targetUrlInput.classList.remove("is-error");
        validationSummary.hidden = true;
        validationSummary.textContent = "";
        resultCard.hidden = true;
        copyResultButton.textContent = "Copy short URL";
        hideQrModal();
    }

    async function copyText(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(text);
            return;
        }

        const helper = document.createElement("textarea");
        helper.value = text;
        helper.setAttribute("readonly", "");
        helper.style.position = "absolute";
        helper.style.left = "-9999px";
        document.body.appendChild(helper);
        helper.select();
        document.execCommand("copy");
        document.body.removeChild(helper);
    }

    function renderValidation(errors) {
        const aliasMessages = errors.Alias ?? errors.alias ?? [];
        const targetMessages = errors.TargetUrl ?? errors.targetUrl ?? [];

        if (aliasMessages.length > 0) {
            aliasError.textContent = aliasMessages[0];
            aliasInput.classList.add("is-error");
        }

        if (targetMessages.length > 0) {
            targetUrlError.textContent = targetMessages[0];
            targetUrlInput.classList.add("is-error");
        }

        const all = [];
        Object.values(errors).forEach(function (msgs) {
            if (Array.isArray(msgs)) {
                msgs.forEach(function (m) { all.push(m); });
            }
        });

        if (all.length > 0) {
            validationSummary.textContent = all.join(" ");
            validationSummary.hidden = false;
        }
    }

    function buildQrCodeUrl(value) {
        return "/api/qr-code?value=" + encodeURIComponent(value);
    }

    function showQrModal() {
        const shortUrl = resultUrl.href;
        qrCodeImage.src = buildQrCodeUrl(shortUrl);
        qrModalLink.href = shortUrl;
        qrModalLink.textContent = shortUrl;
        qrModal.hidden = false;
        document.body.style.overflow = "hidden";
        closeQrButton.focus();
    }

    function hideQrModal() {
        qrModal.hidden = true;
        qrCodeImage.removeAttribute("src");
        qrModalLink.href = "#";
        qrModalLink.textContent = "";
        document.body.style.overflow = "";
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        clearFeedback();

        const aliasValue = aliasInput.value.trim().toLowerCase();
        const aliasClientError = validateAliasValue(aliasValue);
        if (aliasClientError) {
            aliasError.textContent = aliasClientError;
            aliasInput.classList.add("is-error");
            return;
        }

        submitButton.disabled = true;

        try {
            const response = await fetch("/api/redirects", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    alias: aliasInput.value,
                    targetUrl: targetUrlInput.value
                })
            });

            if (response.status === 201) {
                const payload = await response.json();
                resultUrl.href = payload.shortUrl;
                resultUrl.textContent = payload.shortUrl;
                resultCard.hidden = false;
                form.reset();
                return;
            }

            const payload = await response.json().catch(function () {
                return {};
            });

            if (response.status === 400 && payload.errors) {
                renderValidation(payload.errors);
                return;
            }

            if (response.status === 401 || response.status === 403) {
                validationSummary.textContent = "Your session is no longer authorized. Sign in again to continue.";
                validationSummary.hidden = false;
                return;
            }

            validationSummary.textContent = payload.message ?? "The redirect could not be created.";
            validationSummary.hidden = false;
        } finally {
            submitButton.disabled = false;
        }
    });

    copyResultButton.addEventListener("click", async function () {
        try {
            await copyText(resultUrl.href);
            copyResultButton.textContent = "Copied";
        } catch {
            copyResultButton.textContent = "Copy failed";
        }
    });

    showQrButton.addEventListener("click", function () {
        showQrModal();
    });

    closeQrButton.addEventListener("click", function () {
        hideQrModal();
    });

    qrModal.addEventListener("click", function (event) {
        if (event.target === qrModal) {
            hideQrModal();
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && !qrModal.hidden) {
            hideQrModal();
        }
    });
}());
