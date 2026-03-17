FROM quay.io/keycloak/keycloak:26.1 AS builder

ENV KC_DB=mssql \
    KC_HEALTH_ENABLED=true \
    KC_METRICS_ENABLED=true

COPY --chown=keycloak:keycloak themes /opt/keycloak/themes
COPY --chown=keycloak:keycloak realm /opt/keycloak/realm-template

RUN /opt/keycloak/bin/kc.sh build

FROM quay.io/keycloak/keycloak:26.1

ENV KC_HEALTH_ENABLED=true \
    KC_METRICS_ENABLED=true

COPY --from=builder /opt/keycloak/ /opt/keycloak/
COPY --chown=keycloak:keycloak --chmod=755 docker-entrypoint.sh /opt/keycloak/bin/custom-entrypoint.sh

ENTRYPOINT ["/opt/keycloak/bin/custom-entrypoint.sh"]
