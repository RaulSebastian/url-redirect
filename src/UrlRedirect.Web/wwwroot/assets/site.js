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
    const submitButton = document.getElementById("submit-button");

    const RESERVED_ALIASES = new Set(["api", "ui"]);
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

            const message = payload.message ?? "The redirect could not be created.";
            validationSummary.textContent = message;
            validationSummary.hidden = false;
        } finally {
            submitButton.disabled = false;
        }
    });
}());
