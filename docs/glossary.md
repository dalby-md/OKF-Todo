# Glossary

## Installed Contract Test

An automated black-box test whose product inputs are limited to files placed by the Windows installer. For OKF-Todo, it uses the installed OKF bundle, installed GUI command adapter, installed MCP executable, and temporary SQLite databases created through those executables.

## Installation Root

The directory containing the installed OKF-Todo application. Installed contract tests read it from `OKF_TODO_INSTALL_DIR` or default to `%LOCALAPPDATA%\Programs\Okf-Todo`.

## Test Database

A disposable SQLite database created in a test-owned temporary directory by the installed MCP server. It is never the user's normal OKF-Todo database.

## Product Context Boundary

The complete product information available to an installed contract test: installed OKF files, OKF command responses, MCP protocol responses, and observable SQLite state. Repository documentation, source code, application assemblies used as libraries, and internal services are outside this boundary.

## OKF/SQLite Test Path

The supported installed-contract path that reads the installed OKF guidance and invokes `Okf-Todo.exe --okf-command --okf-database-path` against a temporary SQLite database. Mutations use the application adapter so validation and history are preserved.

## OKF-Guided Direct SQLite Capability Test

A deliberately separate test that validates installed OKF table descriptions and then constructs raw SQLite inserts or updates against a disposable database. It proves structural capability while also proving that direct writes bypass application validation and automatic history.

## App Shell

The Photino.NET desktop window hosting the static web UI.

## BodyHtml

The canonical rich text field for an issue. Stored as TinyMCE-produced HTML.

## Image Reference

A stable HTML reference to an image stored in SQLite.

Example:

```html
<img src="app://image/42">
```

## Image Resolver

The code that turns an image reference into renderable image bytes for the editor or viewer. In v1, it should fetch image bytes through the Photino message bridge and use temporary editor URLs while editing.

## Image Import

Adding an image from local user input. In v1, this includes paste, drag/drop, and a static UI or TinyMCE file picker path that stores bytes through `image.create`.

## Issue

The tracked work item. In v1, prefer `Issue` over `Document` unless the product explicitly supports standalone documents.

## Message Bridge

The Photino JavaScript-to-C# communication channel. Application operations should use JSON request/response envelopes instead of ad hoc strings.

## Message Envelope

The JSON shape used on the Photino message bridge. Requests include `messageId`, `type`, and `payload`; responses include the same `messageId`, a result type, `ok`, and either `payload` or `error`.

## REST API

An HTTP-based application API. Out of scope for v1; issue, image, and database operations use the Photino message bridge instead.

## Offline-First

The app can create, edit, save, and reopen issues without network access or external services.

## Sanitizer Allowlist

The explicit set of HTML elements, attributes, CSS properties, and URL schemes accepted from TinyMCE before storing issue HTML.

## Static UI

HTML, CSS, JavaScript, jQuery, and TinyMCE assets stored under `Okf-Todo/wwwroot`. No npm, Vite, Vue, React, or bundler unless explicitly requested.

## Static Asset Loader

The mechanism used by Photino to load packaged HTML, CSS, JavaScript, TinyMCE, and image/icon assets. In v1, use Photino static hosting for packaged assets only.
