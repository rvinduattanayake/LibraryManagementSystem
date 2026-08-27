document.addEventListener('submit', function (event) {
    var form = event.target;
    var message = form.getAttribute('data-confirm');
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});

(function () {
    var sidebar = document.getElementById('sidebar');
    var backdrop = document.getElementById('sidebar-backdrop');
    var openBtn = document.getElementById('sidebar-open');
    var closeBtn = document.getElementById('sidebar-close');
    if (!sidebar || !backdrop || !openBtn) return;

    function open() {
        sidebar.classList.add('is-open');
        backdrop.classList.add('is-open');
        openBtn.setAttribute('aria-expanded', 'true');
    }
    function close() {
        sidebar.classList.remove('is-open');
        backdrop.classList.remove('is-open');
        openBtn.setAttribute('aria-expanded', 'false');
    }

    openBtn.addEventListener('click', open);
    if (closeBtn) closeBtn.addEventListener('click', close);
    backdrop.addEventListener('click', close);
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') close();
    });
})();

(function () {
    var toggle = document.getElementById('theme-toggle');
    if (!toggle) return;
    toggle.addEventListener('click', function () {
        var isDark = document.documentElement.classList.toggle('dark');
        try { localStorage.setItem('theme', isDark ? 'dark' : 'light'); } catch (e) { }
    });
})();
