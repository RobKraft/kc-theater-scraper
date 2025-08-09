// Site JavaScript for KC Theater Web

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Auto-submit form on filter changes
    const filterSelects = document.querySelectorAll('select[name="venue"], select[name="category"]');
    const dateInput = document.querySelector('input[name="date"]');
    
    filterSelects.forEach(select => {
        select.addEventListener('change', function() {
            this.form.submit();
        });
    });
    
    if (dateInput) {
        dateInput.addEventListener('change', function() {
            this.form.submit();
        });
    }
    
    // Initialize tooltips if Bootstrap is loaded
    if (typeof bootstrap !== 'undefined') {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
    
    // Add loading states to buttons
    const actionButtons = document.querySelectorAll('.btn[type="submit"], .btn[href*="search"]');
    actionButtons.forEach(button => {
        button.addEventListener('click', function() {
            if (!this.disabled) {
                const originalContent = this.innerHTML;
                this.innerHTML = '<span class="loading-spinner"></span> Loading...';
                this.disabled = true;
                
                // Re-enable after 5 seconds as a fallback
                setTimeout(() => {
                    this.innerHTML = originalContent;
                    this.disabled = false;
                }, 5000);
            }
        });
    });
});

// Search functionality
function initializeSearch() {
    const searchInput = document.querySelector('input[name="search"]');
    if (searchInput) {
        // Add debounced search
        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                if (this.value.length >= 3 || this.value.length === 0) {
                    this.form.submit();
                }
            }, 500);
        });
    }
}

// Calendar functionality
function navigateCalendar(year, month) {
    window.location.href = `/Home/Calendar?year=${year}&month=${month}`;
}

// Copy to clipboard utility
function copyToClipboard(text) {
    if (navigator.clipboard) {
        return navigator.clipboard.writeText(text);
    } else {
        // Fallback for older browsers
        const textArea = document.createElement("textarea");
        textArea.value = text;
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        try {
            document.execCommand('copy');
            document.body.removeChild(textArea);
            return Promise.resolve();
        } catch (err) {
            document.body.removeChild(textArea);
            return Promise.reject(err);
        }
    }
}

// Theme switcher (for future enhancement)
function toggleTheme() {
    const currentTheme = localStorage.getItem('theme') || 'light';
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
}

// Initialize theme on page load
(function() {
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
})();

// Analytics helper (placeholder for future enhancement)
function trackEvent(category, action, label) {
    if (typeof gtag !== 'undefined') {
        gtag('event', action, {
            event_category: category,
            event_label: label
        });
    }
    console.log(`Analytics: ${category} - ${action} - ${label}`);
}
