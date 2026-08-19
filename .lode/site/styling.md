# Styling

The site uses Tailwind CSS from a CDN. There are no CSS files in the repository. All theme data and all
custom CSS are in `src/_includes/base.njk`.

## Tailwind from CDN

```html
<script src="https://cdn.tailwindcss.com"></script>
<script>
  tailwind.config = { theme: { extend: { colors: { ... } } }, darkMode: 'class' }
</script>
```

The configuration must stay after the CDN script tag. Tailwind reads `tailwind.config` at run time in
the browser.

## Color tokens

All colors use the `oklch()` function. The token names follow the shadcn/ui convention, so a class such
as `bg-card` or `text-muted-foreground` works in any template.

| Token | Value | Use |
|---|---|---|
| `background` / `foreground` | `oklch(1 0 0)` / `oklch(0.145 0 0)` | page base |
| `card` / `card-foreground` | same as background pair | cards and header |
| `primary` | `oklch(0.35 0.15 200)` | teal. Buttons and links. |
| `accent` | `oklch(0.6 0.2 280)` | violet. Gradients and hover. |
| `muted` / `muted-foreground` | `oklch(0.97 0 0)` / `oklch(0.556 0 0)` | secondary text |
| `border` / `input` / `ring` | grey scale | outlines |

## Fonts

- Headings use `Work Sans`, weight 700. The Tailwind family name is `font-serif`.
- Body text uses `Open Sans`. The Tailwind family name is `font-sans`.

Both fonts load from Google Fonts.

## Dark mode

`darkMode: 'class'` is set, and the CSS holds `.dark body` rules. No control adds the `dark` class to
the document. Dark mode is therefore not active. See [backlog](../plans/backlog.md).

## Code colors

Prism.js does the highlighting in the browser. The autoloader plugin gets the C# grammar on demand.
`base.njk` holds a Night Owl color theme as plain CSS token rules, for example:

```css
pre[class*="language-"] { background: #011627; color: #d6deeb; }
.token.keyword { color: #ff6363; }
.token.string  { color: #ecc48d; }
```

The site does no build-time highlighting. The comment at the top of `.eleventy.js` records this
decision. `package.json` does not list a syntax highlight plugin.

Each token rule is written one time. Do not add a second rule for the same token: the last rule wins,
and it hides the Night Owl color.

Related: [Templates and layouts](templates-and-layouts.md).
