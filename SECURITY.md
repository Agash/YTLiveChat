# Security Policy

## Supported versions

Only the latest released version of YTLiveChat receives security fixes. This project is pre-1.0, so
fixes land on the current minor rather than being backported.

## Reporting a vulnerability

**Please do not open a public issue for a security problem.**

Report it privately through GitHub Security Advisories:

1. Go to the [Security tab](../../security/advisories/new) of this repository.
2. Choose **Report a vulnerability**.
3. Describe the issue, the affected version, and how to reproduce it.

You should get an acknowledgement within a few days. Once the issue is confirmed, a fix will be
prepared privately and released together with an advisory crediting you, unless you would rather
stay anonymous.

## Scope

This library talks to third-party services on your behalf. Reports about credential handling, token
storage, request signing, and webhook signature verification are especially welcome. Vulnerabilities
in the upstream service itself should go to that service's own security contact.
