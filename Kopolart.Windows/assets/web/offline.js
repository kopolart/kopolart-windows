/*
 * Offline page behavior: applies theme/language/strings passed via query parameters
 * and posts RETRY_CONNECTION (see AppConstants.Messages.RetryConnection) to the host app.
 */
(function () {
    'use strict';

    var params = new URLSearchParams(window.location.search);
    var isDark = params.get('dark') === '1';

    var countdownTemplate = params.get('countdown') || 'تلاش مجدد خودکار در {0} ثانیه...';

    document.documentElement.lang = 'fa';
    document.documentElement.dir = 'rtl';
    document.body.classList.toggle('dark', isDark);
    document.body.classList.add('fa');

    document.title = params.get('title') || '';
    setText('offline-heading', params.get('heading'));
    setText('offline-text', params.get('text'));
    setText('offline-retry', params.get('button'));

    function setText(id, value) {
        var element = document.getElementById(id);
        if (element && value !== undefined && value !== null) {
            element.textContent = value;
        }
    }

    var countdownInterval = null;
    var secondsLeft = 10;

    function updateCountdown() {
        if (secondsLeft > 0) {
            setText('offline-countdown', countdownTemplate.replace('{0}', secondsLeft));
            secondsLeft--;
        } else {
            clearInterval(countdownInterval);
            countdownInterval = null;
            retry();
        }
    }

    function stopCountdown() {
        if (countdownInterval) {
            clearInterval(countdownInterval);
            countdownInterval = null;
            setText('offline-countdown', '');
        }
    }

    function retry() {
        stopCountdown();
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage('RETRY_CONNECTION');
        }
    }

    document.getElementById('offline-retry').addEventListener('click', retry);

    var countdownEl = document.getElementById('offline-countdown');
    if (countdownEl) {
        countdownEl.addEventListener('click', stopCountdown);
    }

    // Start 10-second automatic retry countdown
    updateCountdown();
    countdownInterval = setInterval(updateCountdown, 1000);

    // Also retry on Space or Enter (same behavior as the previous inline page)
    document.addEventListener('keydown', function (e) {
        if (e.code === 'Space' || e.code === 'Enter') {
            retry();
        }
    });
})();
