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

    function clearFeedback() {
        aliasError.textContent = "";
        targetUrlError.textContent = "";
        validationSummary.hidden = true;
        validationSummary.textContent = "";
        resultCard.hidden = true;
    }

    function renderValidation(errors) {
        const messages = [];

        aliasError.textContent = (errors.Alias ?? errors.alias ?? [])[0] ?? "";
        targetUrlError.textContent = (errors.TargetUrl ?? errors.targetUrl ?? [])[0] ?? "";

        Object.values(errors).forEach(function (fieldMessages) {
            if (Array.isArray(fieldMessages)) {
                fieldMessages.forEach(function (message) {
                    messages.push(message);
                });
            }
        });

        if (messages.length > 0) {
            validationSummary.textContent = messages.join(" ");
            validationSummary.hidden = false;
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        clearFeedback();

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
