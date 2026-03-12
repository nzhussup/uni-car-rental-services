<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
  <#if section == "title">
    Sign in
  <#elseif section == "header">
  <#-- Keep header empty so content remains in one top-down card in form section -->
  <#elseif section == "form">
    <div class="lc-brand">
      <img class="lc-brand-logo" src="${url.resourcesPath}/img/favicon.ico" alt="Leiwand Cars" />
      <span class="lc-brand-text">Leiwand Cars</span>
    </div>
    <h1 class="lc-title">Welcome back</h1>
    <p class="lc-subtitle">Sign in to continue your next ride.</p>

    <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
      <div class="lc-field-group">
        <label for="username" class="lc-label">
          <#if !realm.loginWithEmailAllowed>${msg("username")}
          <#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}
          <#else>${msg("email")}
          </#if>
        </label>
        <input
          id="username"
          class="lc-input"
          name="username"
          value="${(login.username!'')}"
          type="text"
          autofocus
          autocomplete="username"
          aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
        />
        <#if messagesPerField.existsError('username','password')>
          <span class="lc-error" aria-live="polite">${kcSanitize(messagesPerField.getFirstError('username','password'))?no_esc}</span>
        </#if>
      </div>

      <div class="lc-field-group">
        <label for="password" class="lc-label">${msg("password")}</label>
        <div class="lc-password-wrap">
          <input
            id="password"
            class="lc-input lc-input-password"
            name="password"
            type="password"
            autocomplete="current-password"
            aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
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
      </div>

      <div class="lc-options-row">
        <#if realm.rememberMe && !usernameEditDisabled??>
          <label class="lc-check-label">
            <input id="rememberMe" name="rememberMe" type="checkbox" <#if login.rememberMe??>checked</#if> />
            <span>${msg("rememberMe")}</span>
          </label>
        </#if>
        <#if realm.resetPasswordAllowed>
          <a class="lc-link" href="${url.loginResetCredentialsUrl}">${msg("doForgotPassword")}</a>
        </#if>
      </div>

      <input type="hidden" id="id-hidden-input" name="credentialId" <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if> />

      <button class="lc-primary-btn" name="login" id="kc-login" type="submit">${msg("doLogIn")}</button>

      <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
        <p class="lc-footnote">
          New here?
          <a class="lc-link" href="${url.registrationUrl}">${msg("doRegister")}</a>
        </p>
      </#if>
    </form>
  <#elseif section == "info">
  </#if>
</@layout.registrationLayout>
