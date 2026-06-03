/**
 * EventHub - Ana JavaScript Dosyasi
 * Tema yonetimi, animasyonlar ve kullanici etkilesimleri
 */

// ============================================================
// TEMA YONETIMI (Acik / Koyu)
// ============================================================

/**
 * Sayfa yuklendiginde kayitli temay uygular.
 * Tema tercihi localStorage'da saklanir.
 */
(function initTheme() {
    const savedTheme = localStorage.getItem('eventhub-theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeToggleIcon(savedTheme);
})();

/**
 * Tema toggle butonunu tetikler
 */
function toggleTheme() {
    const current = document.documentElement.getAttribute('data-theme') || 'light';
    const next = current === 'light' ? 'dark' : 'light';

    // HTML koku elemana yeni temay uygula
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem('eventhub-theme', next);
    updateThemeToggleIcon(next);
}

/**
 * Toggle butonundaki ikonu gunceller
 */
function updateThemeToggleIcon(theme) {
    const icon = document.getElementById('themeIcon');
    if (icon) {
        icon.innerHTML = theme === 'dark'
            ? '<i class="bi bi-sun-fill" style="font-size:11px;"></i>'
            : '<i class="bi bi-moon-stars-fill" style="font-size:11px;"></i>';
    }
}

// ============================================================
// BILDIRIM (ALERT) YONETIMI
// ============================================================

/**
 * Bildirim kutularini otomatik kapatir (5 saniye sonra)
 */
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert-custom[data-auto-close]');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            closeAlert(alert);
        }, 5000);
    });
});

// ============================================================
// PROFIL PANEL TOGGLE VE SIFRE GOSTER/GIZLE
// ============================================================

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-toggle-panel]').forEach(function (button) {
        button.addEventListener('click', function () {
            var panelId = button.getAttribute('data-toggle-panel');
            var panel = panelId ? document.getElementById(panelId) : null;
            if (!panel) return;

            var wasHidden = panel.classList.contains('d-none');
            panel.classList.toggle('d-none');

            if (panelId === 'profileEditPanel' && panel.classList.contains('d-none')) {
                var passwordPanel = document.getElementById('passwordPanel');
                if (passwordPanel) {
                    passwordPanel.classList.add('d-none');
                }
            }

            var scrollTargetId = button.getAttribute('data-scroll-target');
            if (wasHidden && scrollTargetId) {
                window.setTimeout(function () {
                    var scrollTarget = document.getElementById(scrollTargetId);
                    if (scrollTarget) {
                        scrollTarget.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                }, 80);
            }
        });
    });

    document.querySelectorAll('[data-password-target]').forEach(function (button) {
        button.addEventListener('click', function () {
            var inputId = button.getAttribute('data-password-target');
            var input = inputId ? document.getElementById(inputId) : null;
            if (!input) return;

            var icon = button.querySelector('i');
            var isHidden = input.getAttribute('type') === 'password';
            input.setAttribute('type', isHidden ? 'text' : 'password');

            if (icon) {
                icon.classList.toggle('bi-eye');
                icon.classList.toggle('bi-eye-slash');
            }

            button.setAttribute(
                'aria-label',
                isHidden ? 'Sifreyi gizle' : 'Sifreyi goster'
            );
        });
    });
});

/**
 * Verilen alert elementini kapanma animasyonuyla kaldırir
 */
function closeAlert(element) {
    if (!element) return;
    element.style.transition = 'opacity 0.4s ease, transform 0.4s ease, max-height 0.4s ease';
    element.style.opacity = '0';
    element.style.transform = 'translateY(-8px)';
    element.style.maxHeight = '0';
    element.style.overflow = 'hidden';
    element.style.padding = '0';
    element.style.margin = '0';
    setTimeout(function () {
        element.remove();
    }, 420);
}

// ============================================================
// KAYDIRMA ANIMASYONLARI (Intersection Observer)
// ============================================================

/**
 * Sayfa kaydirildiginda gorunum alanina giren elemanlari canlandırir.
 * .animate-on-scroll sinifina sahip elementler hedef alinir.
 */
(function initScrollAnimations() {
    if (!('IntersectionObserver' in window)) {
        // Eski tarayicilar icin animasyonlari direkt goster
        document.querySelectorAll('.animate-on-scroll').forEach(function (el) {
            el.classList.add('visible');
        });
        return;
    }

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                // Bir kez gorunce izlemeyi durdur (performans)
                observer.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.12,
        rootMargin: '0px 0px -40px 0px'
    });

    document.querySelectorAll('.animate-on-scroll').forEach(function (el) {
        observer.observe(el);
    });
})();

// ============================================================
// SAYAC ANIMASYONU (Istatistik Rakamlari)
// ============================================================

/**
 * Verilen elemandaki sayiyi 0'dan hedef degere animasyonlu sayar
 */
function animateCounter(element, target, duration) {
    var start = 0;
    var step = target / (duration / 16);
    var current = start;

    var timer = setInterval(function () {
        current += step;
        if (current >= target) {
            current = target;
            clearInterval(timer);
        }
        element.textContent = Math.floor(current).toLocaleString('tr-TR');
    }, 16);
}

/**
 * .counter-animate sinifli elemanlari izleyerek gorununce sayaci baslatir
 */
(function initCounterAnimations() {
    if (!('IntersectionObserver' in window)) return;

    const counterObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                var el = entry.target;
                var target = parseInt(el.getAttribute('data-target'), 10);
                if (!isNaN(target)) {
                    animateCounter(el, target, 1200);
                }
                counterObserver.unobserve(el);
            }
        });
    }, { threshold: 0.5 });

    document.querySelectorAll('.counter-animate').forEach(function (el) {
        counterObserver.observe(el);
    });
})();

// ============================================================
// NAVBAR KAYDIRMA EFEKTI
// ============================================================

/**
 * Sayfa asagi kaydirildikca navbar'a golge ekler
 */
(function initNavbarScroll() {
    var navbar = document.querySelector('.navbar-eventhub');
    if (!navbar) return;

    window.addEventListener('scroll', function () {
        if (window.scrollY > 20) {
            navbar.style.boxShadow = '0 4px 24px rgba(0,0,0,0.1)';
        } else {
            navbar.style.boxShadow = 'none';
        }
    }, { passive: true });
})();

// ============================================================
// FORM DOGRULAMA GERI BILDIRIMLERI
// ============================================================

/**
 * Form submit oncesi yuklenme durumu gosterir
 */
document.addEventListener('DOMContentLoaded', function () {
    var forms = document.querySelectorAll('form[data-loading]');
    forms.forEach(function (form) {
        form.addEventListener('submit', function () {
            var btn = form.querySelector('button[type="submit"]');
            if (btn) {
                btn.disabled = true;
                var originalText = btn.innerHTML;
                btn.innerHTML = '<span class="loading-spinner" style="display:inline-block;width:16px;height:16px;border-width:2px;vertical-align:middle;margin-right:6px;"></span>Isleniyor...';
                // 8 saniye sonra butonu tekrar aktif et (sayfa donmesi durumu)
                setTimeout(function () {
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }, 8000);
            }
        });
    });
});

// ============================================================
// SILME ONAY DIALOGU
// ============================================================

/**
 * data-confirm attribute'lu butonlar icin onay istegi gonderir
 */
document.addEventListener('DOMContentLoaded', function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-confirm]');
        if (!btn) return;

        var message = btn.getAttribute('data-confirm') || 'Bu islemi gerceklestirmek istediginizden emin misiniz?';
        if (!confirm(message)) {
            e.preventDefault();
            e.stopPropagation();
        }
    });
});

// ============================================================
// KONTENJAN PROGRESS BAR CANLANDIRMASI
// ============================================================

/**
 * Sayfa yuklenince kontenjan cubukları genisletir
 */
document.addEventListener('DOMContentLoaded', function () {
    var bars = document.querySelectorAll('.capacity-bar-fill');
    bars.forEach(function (bar) {
        var targetWidth = bar.style.width;
        bar.style.width = '0';
        setTimeout(function () {
            bar.style.width = targetWidth;
        }, 300);
    });
});

// ============================================================
// KOPYA BUTONU (Online Etkinlik URL)
// ============================================================

/**
 * URL kopyalama butonu islevi
 */
function copyToClipboard(text) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(function () {
            showToast('Baglanti kopyalandi!', 'success');
        });
    } else {
        // Eski tarayici fallback
        var el = document.createElement('textarea');
        el.value = text;
        document.body.appendChild(el);
        el.select();
        document.execCommand('copy');
        document.body.removeChild(el);
        showToast('Baglanti kopyalandi!', 'success');
    }
}

// ============================================================
// TOAST BILDIRIMI
// ============================================================

/**
 * Sag alt kose toast bildirimi gosterir
 */
function showToast(message, type) {
    type = type || 'info';

    var icons = {
        success: '',
        error: '',
        info: '',
        warning: ''
    };

    var colors = {
        success: 'var(--color-success)',
        error: 'var(--color-danger)',
        info: 'var(--color-info)',
        warning: 'var(--color-warning)'
    };

    var toast = document.createElement('div');
    toast.style.cssText = [
        'position:fixed',
        'bottom:24px',
        'right:24px',
        'z-index:9999',
        'background:var(--bg-card)',
        'border:1px solid var(--border-color)',
        'border-left:4px solid ' + colors[type],
        'border-radius:var(--border-radius-md)',
        'padding:0.9rem 1.2rem',
        'display:flex',
        'align-items:center',
        'gap:0.6rem',
        'box-shadow:var(--shadow-lg)',
        'font-size:0.9rem',
        'font-weight:500',
        'color:var(--text-primary)',
        'max-width:320px',
        'animation:slide-down 0.3s ease',
        'transition:all 0.3s ease'
    ].join(';');

    toast.innerHTML = '<span style="color:' + colors[type] + '">' + icons[type] + '</span>' + message;
    document.body.appendChild(toast);

    setTimeout(function () {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(12px)';
        setTimeout(function () { toast.remove(); }, 320);
    }, 3500);
}

// ============================================================
// FILTRE FORMU - ANLIK ARAMA
// ============================================================

/**
 * Arama kutusunda 400ms bekleme sonrasi formu submit eder
 */
(function initLiveSearch() {
    var searchInput = document.getElementById('searchInput');
    if (!searchInput) return;

    var searchTimer;
    searchInput.addEventListener('input', function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () {
            searchInput.closest('form').submit();
        }, 500);
    });
})();
