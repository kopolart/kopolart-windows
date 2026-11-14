/*
 * Forwards app keyboard shortcuts from the page to the host via postMessage.
 *
 * Replaces the previous reflection into WebView2's private _coreWebView2Controller
 * field (a single point of failure that silently killed every in-page shortcut when
 * the package's internals changed). Runs in the capture phase and swallows handled
 * combinations, mirroring the old browser-level AcceleratorKeyPressed behavior.
 *
 * Handled (must stay in sync with KeyboardShortcutHandler.ProcessKey):
 *   F5, Ctrl+R, Ctrl+Shift+R, Ctrl+F, Ctrl+D, Ctrl+H, Alt+Left, Alt+Right, Escape.
 */
(function () {
    'use strict';

    function isHandledShortcut(e) {
        var code = e.code;

        if (code === 'F5' || code === 'Escape') {
            return !e.ctrlKey && !e.altKey && !e.metaKey && !e.shiftKey;
        }

        if (e.ctrlKey && !e.altKey && !e.metaKey) {
            if (code === 'KeyR') {
                return true; // Ctrl+R and Ctrl+Shift+R
            }
            if (code === 'KeyF' || code === 'KeyD' || code === 'KeyH') {
                return !e.shiftKey;
            }
        }

        if (e.altKey && !e.ctrlKey && !e.metaKey && !e.shiftKey &&
            (code === 'ArrowLeft' || code === 'ArrowRight')) {
            return true;
        }

        return false;
    }

    document.addEventListener('keydown', function (e) {
        if (!isHandledShortcut(e)) {
            return;
        }

        e.preventDefault();
        e.stopPropagation();

        try {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage('KP_SHORTCUT:' + JSON.stringify({
                    code: e.code,
                    ctrl: e.ctrlKey,
                    shift: e.shiftKey,
                    alt: e.altKey
                }));
            }
        } catch (err) {
            // Host bridge unavailable — ignore.
        }
    }, true);

    // Notify the host application when the user clicks anywhere in the web page
    // so any open dropdown/sub-menus can be automatically dismissed.
    function notifyPageClick() {
        try {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage('KP_PAGE_CLICK');
            }
        } catch (err) {
            // Host bridge unavailable — ignore.
        }
    }

    window.addEventListener('pointerdown', notifyPageClick, true);
    window.addEventListener('mousedown', notifyPageClick, true);
    document.addEventListener('pointerdown', notifyPageClick, true);
    document.addEventListener('mousedown', notifyPageClick, true);
})();

