# 📚 Local Documentation Preview

Preview the GitHub Pages documentation locally before pushing changes.

## 🚀 Quick Start

### Using Docker Compose (Recommended)

**Prerequisites:**
- Docker Desktop installed and **running**
- Docker Compose installed (included with Docker Desktop)

**Start the server:**

```bash
cd gh-pages
docker-compose up
```

**First run takes ~2-3 minutes** (downloads Ruby image + installs Jekyll)  
**Subsequent runs are much faster!** ⚡

**Access the documentation:**
```
http://localhost:4000/mcp.gateway/
```

**Stop the server:**
```bash
docker-compose down
```

Or press `Ctrl+C` in the terminal.

---

## ✨ Features

- ✅ **Live reload** - Changes are reflected automatically
- ✅ **Fast rebuild** - Jekyll watches for file changes
- ✅ **Same as GitHub Pages** - Uses same Jekyll version and theme
- ✅ **No local Ruby/Jekyll** - Everything runs in Docker

---

## 📝 Workflow

1. **Start Docker Compose:**
   ```bash
   cd gh-pages
   docker-compose up
   ```

2. **Edit documentation:**
   - Edit any `.md` file in `gh-pages/`
   - Save the file

3. **See changes:**
   - Browser automatically refreshes (LiveReload)
   - Or manually refresh: `http://localhost:4000/mcp.gateway/`

4. **Commit when satisfied:**
   ```bash
   git add .
   git commit -m "docs: Update XYZ"
   git push origin docs/github-pages
   ```

---

## 🔧 Configuration

### Change port

Edit `docker-compose.yml`:

```yaml
ports:
  - "8080:4000"  # Change 4000 to 8080
```

Access at: `http://localhost:8080/mcp.gateway/`

### Clean build

If things look weird, rebuild from scratch:

```bash
docker-compose down
docker-compose up --build
```

---

## 🐛 Troubleshooting

### Docker Desktop not running

**Error:**
```
error during connect: Get "http://%2F%2F.%2Fpipe%2FdockerDesktopLinuxEngine/..."
The system cannot find the file specified
```

**Solution:**
1. **Start Docker Desktop** (Windows taskbar icon)
2. Wait until Docker icon shows "Running"
3. Try again: `docker-compose up`

### Port already in use

**Error:**
```
Bind for 0.0.0.0:4000 failed: port is already allocated
```

**Solution:**
1. Stop other services using port 4000
2. Or change port in `docker-compose.yml`

### Changes not reflected

**Try:**
1. Hard refresh: `Ctrl+F5` (Windows) or `Cmd+Shift+R` (Mac)
2. Clear browser cache
3. Restart Docker Compose:
   ```bash
   docker-compose down
   docker-compose up
   ```

### First run takes time

**Expected behavior:**
- Downloads Ruby image (~50 MB - Alpine Linux based)
- Installs Jekyll and dependencies (~2-3 minutes first time)
- **Subsequent runs use cached image and gems** ⚡
- Server starts in ~10 seconds after first run

### Build errors

**Error:**
```
An error occurred while installing X
```

**Solution:**
Rebuild from scratch:
```bash
docker-compose down
docker-compose up --build --force-recreate
```

---

## 📂 Directory Structure

```
gh-pages/
├── docker-compose.yml       # Docker Compose configuration
├── PREVIEW.md               # This file
├── _config.yml              # Jekyll configuration
├── _getting-started/        # Getting Started pages
├── _examples/               # Example pages
├── _features/               # Feature pages
├── _api/                    # API reference pages
├── _layouts/                # Jekyll layouts
├── _includes/               # Jekyll includes
├── assets/                  # CSS, JS, images
└── index.md                 # Home page
```

---

## 🎯 What You'll See

When you run `docker-compose up`, you'll see:

```
[+] Running 1/0
 ✔ Container mcp-gateway-docs  Created
Attaching to mcp-gateway-docs
mcp-gateway-docs | Fetching gem metadata from https://rubygems.org/........
mcp-gateway-docs | Resolving dependencies...
mcp-gateway-docs | Installing jekyll 4.3.x
mcp-gateway-docs | ...
mcp-gateway-docs | Bundle complete!
mcp-gateway-docs | Configuration file: /srv/jekyll/_config.yml
mcp-gateway-docs |             Source: /srv/jekyll
mcp-gateway-docs |        Destination: /srv/jekyll/_site
mcp-gateway-docs |  Incremental build: disabled. Enable with --incremental
mcp-gateway-docs |       Generating... 
mcp-gateway-docs |        Jekyll Feed: Generating feed for posts
mcp-gateway-docs |                     done in 2.3 seconds.
mcp-gateway-docs |  Auto-regeneration: enabled for '/srv/jekyll'
mcp-gateway-docs | LiveReload address: http://0.0.0.0:35729
mcp-gateway-docs |     Server address: http://0.0.0.0:4000/mcp.gateway/
mcp-gateway-docs |   Server running... press ctrl-c to stop.
```

**Key line:**
```
Server address: http://0.0.0.0:4000/mcp.gateway/
```

**When you see this, open your browser!** 🎉

**Note:** First run shows gem installation. Subsequent runs skip straight to server startup!

---

## 🔗 URLs to Test

Once server is running, test these pages:

- **Home:** http://localhost:4000/mcp.gateway/
- **AI Quickstart:** http://localhost:4000/mcp.gateway/getting-started/ai-quickstart/
- **Getting Started:** http://localhost:4000/mcp.gateway/getting-started/index/
- **Calculator Example:** http://localhost:4000/mcp.gateway/examples/calculator/
- **Tools API:** http://localhost:4000/mcp.gateway/api/tools/
- **Features:** http://localhost:4000/mcp.gateway/features/lifecycle-hooks/

---

## 💡 Tips

### Edit and watch

1. Open browser: `http://localhost:4000/mcp.gateway/`
2. Open editor: Edit any `.md` file
3. Save file
4. **Browser auto-refreshes!** ✨

### Test navigation

- Click through all menu items
- Check breadcrumbs work
- Verify all "See Also" links
- Test search (if enabled)

### Test dark mode

Toggle dark mode in browser and verify:
- Colors look good
- No flash of unstyled content
- All elements visible

---

## 🎉 That's it!

You now have a local preview environment for documentation!

**No more blind commits!** 🙈

**See changes before GitHub Pages builds!** 👀

**Happy documenting!** 📚✨
