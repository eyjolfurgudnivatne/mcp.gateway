# MCP Gateway Documentation Site

This directory contains the GitHub Pages site for MCP Gateway.

## 🌐 Live Site

**URL:** https://eyjolfurgudnivatne.github.io/mcp.gateway/

## 📁 Structure

```
gh-pages/
├── _config.yml          # Jekyll configuration
├── index.html           # Landing page
├── _layouts/            # Jekyll layouts
│   └── default.html
├── _includes/           # Reusable components
│   ├── header.html
│   └── footer.html
├── assets/              # Static assets
│   ├── css/
│   │   └── main.css
│   ├── js/
│   │   └── main.js
│   └── images/
├── getting-started/     # Getting started guides
├── features/            # Feature documentation
├── api-reference/       # API reference
└── examples/            # Examples
```

## 🚀 Local Development

### Prerequisites
- Ruby (2.7+)
- Bundler

### Setup

```bash
# Install Jekyll and dependencies
gem install bundler jekyll

# Navigate to gh-pages directory
cd gh-pages

# Install dependencies (if Gemfile exists)
bundle install

# Serve locally
bundle exec jekyll serve

# Or use Jekyll directly
jekyll serve --baseurl "/mcp.gateway"
```

Visit: http://localhost:4000/mcp.gateway/

## 📝 Adding Content

### Create a New Page

```bash
# Create markdown file in appropriate directory
touch getting-started/your-page.md
```

Example page:

```markdown
---
layout: default
title: Your Page Title
description: Page description
---

# Your Page Title

Your content here...
```

### Update Navigation

Edit `_config.yml` and add to `navigation`:

```yaml
navigation:
  - title: Your Page
    url: /your-page/
```

## 🎨 Customization

### Styling

- Main styles: `assets/css/main.css`
- Modify CSS variables in `:root` for theme customization

### Layout

- Default layout: `_layouts/default.html`
- Header: `_includes/header.html`
- Footer: `_includes/footer.html`

### JavaScript

- Main script: `assets/js/main.js`
- Features: mobile nav, smooth scrolling, code copy button

## 🚢 Deployment

GitHub Pages automatically builds and deploys when you push to `docs/github-pages` branch.

### Manual Deployment

```bash
# Build site
bundle exec jekyll build

# Output in _site/ directory
```

### GitHub Actions (Optional)

Create `.github/workflows/gh-pages.yml` for automated deployment:

```yaml
name: Deploy GitHub Pages

on:
  push:
    branches: [docs/github-pages]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Ruby
        uses: ruby/setup-ruby@v1
        with:
          ruby-version: '3.1'
          bundler-cache: true
          
      - name: Setup Pages
        uses: actions/configure-pages@v3
        
      - name: Build with Jekyll
        run: |
          cd gh-pages
          bundle exec jekyll build --baseurl "/mcp.gateway"
        env:
          JEKYLL_ENV: production
          
      - name: Upload artifact
        uses: actions/upload-pages-artifact@v2
        with:
          path: gh-pages/_site
          
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v2
```

## 📚 Content Guidelines

### Writing Style
- Clear and concise
- Use code examples
- Include links to related content
- Follow existing page structure

### Code Examples
- Use syntax highlighting
- Include comments
- Show complete, runnable examples
- Test all examples

### Images
- Place in `assets/images/`
- Use descriptive filenames
- Optimize for web (compress)
- Include alt text

## 🛠️ Troubleshooting

### Jekyll Not Found
```bash
gem install jekyll bundler
```

### Build Errors
```bash
# Clear cache
bundle exec jekyll clean

# Rebuild
bundle exec jekyll build
```

### CSS Not Loading
Check `baseurl` in `_config.yml` matches your repository name.

## 📖 Resources

- [Jekyll Documentation](https://jekyllrb.com/docs/)
- [GitHub Pages Documentation](https://docs.github.com/en/pages)
- [Kramdown Syntax](https://kramdown.gettalong.org/syntax.html)

## 🤝 Contributing

See main [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines.

## 📜 License

MIT © 2024-2025 ARKo AS - AHelse Development Team
