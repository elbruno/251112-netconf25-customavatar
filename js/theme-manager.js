// Theme Manager - Light/Dark/System Mode
(function() {
    'use strict';
    
    const THEME_KEY = 'preferred-theme';
    const THEMES = {
        LIGHT: 'light',
        DARK: 'dark',
        SYSTEM: 'system'
    };
    
    let currentTheme = THEMES.SYSTEM;
    
    // Get system preference
    function getSystemTheme() {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? THEMES.DARK : THEMES.LIGHT;
    }
    
    // Apply theme to document
    function applyTheme(theme) {
        const actualTheme = theme === THEMES.SYSTEM ? getSystemTheme() : theme;
        document.documentElement.setAttribute('data-theme', actualTheme);
        
        // Update button icon and text
        const btn = document.getElementById('themeToggle');
        const icon = document.getElementById('themeIcon');
        const text = document.getElementById('themeText');
        
        if (btn && icon && text) {
            switch(theme) {
                case THEMES.LIGHT:
                    icon.className = 'bi bi-sun-fill';
                    text.textContent = 'Light';
                    break;
                case THEMES.DARK:
                    icon.className = 'bi bi-moon-stars-fill';
                    text.textContent = 'Dark';
                    break;
                case THEMES.SYSTEM:
                    icon.className = 'bi bi-circle-half';
                    text.textContent = 'Auto';
                    break;
            }
        }
        
        console.log(`Theme applied: ${theme} (actual: ${actualTheme})`);
    }
    
    // Load saved theme preference
    function loadTheme() {
        try {
            const saved = localStorage.getItem(THEME_KEY);
            if (saved && Object.values(THEMES).includes(saved)) {
                currentTheme = saved;
            }
        } catch (e) {
            console.warn('Could not load theme preference:', e);
        }
        applyTheme(currentTheme);
    }
    
    // Save theme preference
    function saveTheme(theme) {
        try {
            localStorage.setItem(THEME_KEY, theme);
        } catch (e) {
            console.warn('Could not save theme preference:', e);
        }
    }
    
    // Cycle through themes
    function cycleTheme() {
        const themes = [THEMES.SYSTEM, THEMES.LIGHT, THEMES.DARK];
        const currentIndex = themes.indexOf(currentTheme);
        const nextIndex = (currentIndex + 1) % themes.length;
        currentTheme = themes[nextIndex];
        
        saveTheme(currentTheme);
        applyTheme(currentTheme);
        
        // Add a subtle animation
        const btn = document.getElementById('themeToggle');
        if (btn) {
            btn.style.transform = 'scale(0.9)';
            setTimeout(() => {
                btn.style.transform = '';
            }, 150);
        }
    }
    
    // Listen for system theme changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        if (currentTheme === THEMES.SYSTEM) {
            applyTheme(THEMES.SYSTEM);
        }
    });
    
    // Initialize on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', loadTheme);
    } else {
        loadTheme();
    }
    
    // Expose global function for button click
    window.toggleTheme = cycleTheme;
    
    // Export for other scripts if needed
    window.ThemeManager = {
        getCurrentTheme: () => currentTheme,
        setTheme: (theme) => {
            if (Object.values(THEMES).includes(theme)) {
                currentTheme = theme;
                saveTheme(theme);
                applyTheme(theme);
            }
        },
        getActualTheme: () => {
            return currentTheme === THEMES.SYSTEM ? getSystemTheme() : currentTheme;
        }
    };
    
    console.log('🎨 Theme Manager initialized');
})();
