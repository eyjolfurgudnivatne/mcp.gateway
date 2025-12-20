// MCP Gateway - MCP-inspired Interactive Features

document.addEventListener('DOMContentLoaded', function() {
  // Theme Toggle
  initThemeToggle();
  
  // Mobile Menu
  initMobileMenu();
  
  // Active Navigation
  initActiveNav();
  
  // Table of Contents
  initTableOfContents();
  
  // Smooth Scrolling
  initSmoothScrolling();
  
  // Code Copy Buttons
  initCodeCopyButtons();
});

// Theme Toggle
function initThemeToggle() {
  const themeToggle = document.getElementById('themeToggle');
  const html = document.documentElement;
  
  // Load saved theme or default to light
  const savedTheme = localStorage.getItem('theme') || 'light';
  html.setAttribute('data-theme', savedTheme);
  
  if (themeToggle) {
    themeToggle.addEventListener('click', function() {
      const currentTheme = html.getAttribute('data-theme');
      const newTheme = currentTheme === 'light' ? 'dark' : 'light';
      
      html.setAttribute('data-theme', newTheme);
      localStorage.setItem('theme', newTheme);
      
      // Smooth transition
      html.style.transition = 'background-color 200ms, color 200ms';
      setTimeout(() => {
        html.style.transition = '';
      }, 200);
    });
  }
}

// Mobile Menu
function initMobileMenu() {
  const menuToggle = document.getElementById('mobileMenuToggle');
  const sidebar = document.getElementById('sidebar');
  const mainContent = document.getElementById('mainContent');
  
  if (menuToggle && sidebar) {
    menuToggle.addEventListener('click', function(e) {
      e.stopPropagation();
      sidebar.classList.toggle('open');
    });
    
    // Close menu when clicking outside
    document.addEventListener('click', function(e) {
      if (!sidebar.contains(e.target) && !menuToggle.contains(e.target)) {
        sidebar.classList.remove('open');
      }
    });
    
    // Close menu when clicking a link
    const navLinks = sidebar.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
      link.addEventListener('click', function() {
        sidebar.classList.remove('open');
      });
    });
  }
}

// Active Navigation
function initActiveNav() {
  const currentPath = window.location.pathname;
  const navLinks = document.querySelectorAll('.nav-link');
  
  navLinks.forEach(link => {
    const linkPath = link.getAttribute('href');
    if (linkPath === currentPath || currentPath.startsWith(linkPath + '/')) {
      link.classList.add('active');
      
      // Expand parent section
      const section = link.closest('.nav-section');
      if (section) {
        section.classList.add('expanded');
      }
    }
  });
}

// Table of Contents
function initTableOfContents() {
  const tocNav = document.getElementById('tocNav');
  const content = document.querySelector('.content');
  
  if (!tocNav || !content) return;
  
  // Find all headings
  const headings = content.querySelectorAll('h2, h3');
  
  if (headings.length === 0) {
    document.querySelector('.toc')?.remove();
    return;
  }
  
  // Build TOC
  const tocList = document.createElement('ul');
  tocList.className = 'toc-list';
  
  headings.forEach((heading, index) => {
    // Add ID if missing
    if (!heading.id) {
      heading.id = `heading-${index}`;
    }
    
    const li = document.createElement('li');
    li.className = heading.tagName === 'H3' ? 'toc-item toc-item-sub' : 'toc-item';
    
    const a = document.createElement('a');
    a.href = `#${heading.id}`;
    a.textContent = heading.textContent;
    a.className = 'toc-link';
    
    li.appendChild(a);
    tocList.appendChild(li);
  });
  
  tocNav.appendChild(tocList);
  
  // Highlight active section on scroll
  window.addEventListener('scroll', () => {
    let current = '';
    
    headings.forEach(heading => {
      const sectionTop = heading.offsetTop;
      if (window.pageYOffset >= sectionTop - 100) {
        current = heading.id;
      }
    });
    
    tocNav.querySelectorAll('.toc-link').forEach(link => {
      link.classList.remove('active');
      if (link.getAttribute('href') === `#${current}`) {
        link.classList.add('active');
      }
    });
  });
}

// Smooth Scrolling
function initSmoothScrolling() {
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function(e) {
      e.preventDefault();
      
      const targetId = this.getAttribute('href');
      if (targetId === '#') return;
      
      const target = document.querySelector(targetId);
      if (target) {
        const offset = 80; // Account for fixed header
        const targetPosition = target.offsetTop - offset;
        
        window.scrollTo({
          top: targetPosition,
          behavior: 'smooth'
        });
        
        // Update URL without jumping
        history.pushState(null, null, targetId);
      }
    });
  });
}

// Code Copy Buttons
function initCodeCopyButtons() {
  document.querySelectorAll('pre code').forEach((codeBlock) => {
    const button = document.createElement('button');
    button.className = 'copy-button';
    button.innerHTML = '<i data-feather="copy"></i>';
    button.title = 'Copy code';
    
    const pre = codeBlock.parentElement;
    pre.style.position = 'relative';
    pre.appendChild(button);
    
    button.addEventListener('click', async function() {
      const text = codeBlock.textContent;
      
      try {
        await navigator.clipboard.writeText(text);
        button.innerHTML = '<i data-feather="check"></i>';
        button.classList.add('copied');
        
        setTimeout(() => {
          button.innerHTML = '<i data-feather="copy"></i>';
          button.classList.remove('copied');
          feather.replace();
        }, 2000);
      } catch (err) {
        console.error('Failed to copy:', err);
      }
      
      feather.replace();
    });
  });
  
  feather.replace();
}

// Add copy button styles dynamically
const style = document.createElement('style');
style.textContent = `
  .copy-button {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    padding: 0.5rem;
    background: rgba(255, 255, 255, 0.1);
    border: 1px solid rgba(255, 255, 255, 0.2);
    border-radius: 6px;
    cursor: pointer;
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
    transition: all 150ms;
  }

  .copy-button:hover {
    background: rgba(255, 255, 255, 0.2);
  }

  .copy-button.copied {
    background: rgba(34, 197, 94, 0.2);
    border-color: rgba(34, 197, 94, 0.3);
    color: #22c55e;
  }

  .copy-button svg {
    width: 16px;
    height: 16px;
  }
`;
document.head.appendChild(style);
