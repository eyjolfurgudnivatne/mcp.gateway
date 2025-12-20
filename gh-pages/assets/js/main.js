// MCP Gateway - Main JavaScript

document.addEventListener('DOMContentLoaded', function() {
  // Mobile Navigation Toggle
  const navToggle = document.querySelector('.nav-toggle');
  const navMenu = document.querySelector('.nav-menu');

  if (navToggle) {
    navToggle.addEventListener('click', function() {
      navMenu.classList.toggle('active');
    });
  }

  // Close mobile menu when clicking outside
  document.addEventListener('click', function(event) {
    if (!event.target.closest('.site-nav')) {
      navMenu.classList.remove('active');
    }
  });

  // Smooth Scrolling for Anchor Links
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function(e) {
      e.preventDefault();
      const target = document.querySelector(this.getAttribute('href'));
      if (target) {
        target.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });
      }
    });
  });

  // Add Copy Button to Code Blocks
  document.querySelectorAll('pre code').forEach(function(codeBlock) {
    const button = document.createElement('button');
    button.className = 'copy-button';
    button.textContent = 'Copy';
    
    const pre = codeBlock.parentElement;
    pre.style.position = 'relative';
    pre.appendChild(button);

    button.addEventListener('click', function() {
      const text = codeBlock.textContent;
      navigator.clipboard.writeText(text).then(function() {
        button.textContent = 'Copied!';
        setTimeout(function() {
          button.textContent = 'Copy';
        }, 2000);
      });
    });
  });

  // Add Active Class to Current Nav Item
  const currentPath = window.location.pathname;
  document.querySelectorAll('.nav-link').forEach(link => {
    if (link.getAttribute('href') === currentPath) {
      link.classList.add('active');
    }
  });
});

// Copy Button Styles (inject dynamically)
const style = document.createElement('style');
style.textContent = `
  .copy-button {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    padding: 0.25rem 0.5rem;
    background: rgba(255,255,255,0.1);
    color: white;
    border: 1px solid rgba(255,255,255,0.2);
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.75rem;
    transition: all 0.3s;
  }

  .copy-button:hover {
    background: rgba(255,255,255,0.2);
  }

  .nav-link.active {
    color: var(--primary-color);
    border-bottom: 2px solid var(--primary-color);
  }
`;
document.head.appendChild(style);
