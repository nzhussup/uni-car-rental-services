<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password','password-confirm','email','firstName','lastName'); section>
  <#if section == "title">
    Register
  <#elseif section == "header">
  <#-- Keep header empty so content remains in one top-down card in form section -->
  <#elseif section == "form">
    <div class="lc-brand">
      <img class="lc-brand-logo" src="${url.resourcesPath}/img/favicon.ico" alt="Leiwand Cars" />
      <span class="lc-brand-text">Leiwand Cars</span>
    </div>
    <h1 class="lc-title">Create your account</h1>
    <p class="lc-subtitle">Join Leiwand Cars and book your next ride faster.</p>

    <form id="kc-register-form" action="${url.registrationAction}" method="post">
      <div class="lc-grid-2">
        <div class="lc-field-group">
          <label for="firstName" class="lc-label">${msg("firstName")}</label>
          <input
            type="text"
            id="firstName"
            class="lc-input"
            name="firstName"
            value="${(register.formData.firstName!'')}"
            autocomplete="given-name"
            aria-invalid="<#if messagesPerField.existsError('firstName')>true</#if>"
          />
          <#if messagesPerField.existsError('firstName')>
            <span class="lc-error" aria-live="polite">${kcSanitize(messagesPerField.getFirstError('firstName'))?no_esc}</span>
          </#if>
        </div>

        <div class="lc-field-group">
          <label for="lastName" class="lc-label">${msg("lastName")}</label>
          <input
            type="text"
            id="lastName"
            class="lc-input"
            name="lastName"
            value="${(register.formData.lastName!'')}"
            autocomplete="family-name"
            aria-invalid="<#if messagesPerField.existsError('lastName')>true</#if>"
          />
          <#if messagesPerField.existsError('lastName')>
            <span class="lc-error" aria-live="polite">${kcSanitize(messagesPerField.getFirstError('lastName'))?no_esc}</span>
          </#if>
        </div>
      </div>

      <div class="lc-field-group">
        <label for="email" class="lc-label">${msg("email")}</label>
        <input
          type="email"
          id="email"
          class="lc-input"
          name="email"
          value="${(register.formData.email!'')}"
          autocomplete="email"
          aria-invalid="<#if messagesPerField.existsError('email')>true</#if>"
        />
        <#if messagesPerField.existsError('email')>
          <span class="lc-error" aria-live="polite">${kcSanitize(messagesPerField.getFirstError('email'))?no_esc}</span>
        </#if>
      </div>

      <#if !realm.registrationEmailAsUsername>
        <div class="lc-field-group">
          <label for="username" class="lc-label">${msg("username")}</label>
          <input
            type="text"
            id="username"
            class="lc-input"
            name="username"
            value="${(register.formData.username!'')}"
            autocomplete="username"
            aria-invalid="<#if messagesPerField.existsError('username')>true</#if>"
          />
          <#if messagesPerField.existsError('username')>
            <span class="lc-error" aria-live="polite">${kcSanitize(messagesPerField.getFirstError('username'))?no_esc}</span>
          </#if>
        </div>
      </#if>

      <#if passwordRequired??>
        <div class="lc-grid-2">
          <div class="lc-field-group">
            <label for="password" class="lc-label">${msg("password")}</label>
            <div class="lc-password-wrap">
              <input
                type="password"
                id="password"
                class="lc-input lc-input-password"
                name="password"
                autocomplete="new-password"
                aria-invalid="<#if messagesPerField.existsError('password','password-confirm')>true</#if>"
              />
              <button
                type="button"
                class="lc-password-toggle"
                data-password-toggle
                data-target="password"
                aria-label="Show password"
              >
                <svg class="lc-eye-open" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                  <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="1.8" />
                </svg>
                <svg class="lc-eye-off" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M3 3l18 18" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                  <path d="M10.6 6.2A11.5 11.5 0 0 1 12 6c6.5 0 10 6 10 6a16.6 16.6 0 0 1-3 3.6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                  <path d="M6.5 7.7C3.9 9.6 2 12 2 12s3.5 6 10 6c1.8 0 3.3-.4 4.7-1" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                  <path d="M9.9 9.8A3 3 0 0 0 14.2 14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </button>
            </div>
            <#if messagesPerField.existsError('password')>
              <span class="lc-error" aria-live="polite">${kcSanitize(messagesPerField.getFirstError('password'))?no_esc}</span>
            </#if>
          </div>

          <div class="lc-field-group">
            <label for="password-confirm" class="lc-label">${msg("passwordConfirm")}</label>
            <div class="lc-password-wrap">
              <input
                type="password"
                id="password-confirm"
                class="lc-input lc-input-password"
                name="password-confirm"
                autocomplete="new-password"
                aria-invalid="<#if messagesPerField.existsError('password-confirm')>true</#if>"
              />
              <button
                type="button"
                class="lc-password-toggle"
                data-password-toggle
                data-target="password-confirm"
                aria-label="Show password"
              >
                <svg class="lc-eye-open" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6Z" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                  <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="1.8" />
                </svg>
                <svg class="lc-eye-off" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M3 3l18 18" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                  <path d="M10.6 6.2A11.5 11.5 0 0 1 12 6c6.5 0 10 6 10 6a16.6 16.6 0 0 1-3 3.6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                  <path d="M6.5 7.7C3.9 9.6 2 12 2 12s3.5 6 10 6c1.8 0 3.3-.4 4.7-1" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                  <path d="M9.9 9.8A3 3 0 0 0 14.2 14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </button>
            </div>
            <#if messagesPerField.existsError('password-confirm')>
              <span class="lc-error" aria-live="polite">${kcSanitize(messagesPerField.getFirstError('password-confirm'))?no_esc}</span>
            </#if>
          </div>
        </div>
      </#if>

      <div class="lc-actions-row">
        <a class="lc-secondary-btn" href="${url.loginUrl}">Back</a>
        <button class="lc-primary-btn" id="kc-register" type="submit">${msg("doRegister")}</button>
      </div>
    </form>
  <#elseif section == "info">
  </#if>
</@layout.registrationLayout>
