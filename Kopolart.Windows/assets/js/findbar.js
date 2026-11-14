/*
 * Find-in-page bar. Reads window.__kopolartFindBarConfig (set by the host app):
 *   { isDark, isRtl, placeholder, nextText, prevText, css }
 *
 * Idempotent by design: the first run creates the bar and stores a small API on
 * window.__kopolartFindBar; every later run only calls that API. The previous inline
 * version re-entered stale closures from an old script scope on the second Ctrl+F,
 * threw a TypeError, and left the bar stuck open.
 */
(function () {
    'use strict';

    var config = window.__kopolartFindBarConfig || {};

    if (window.__kopolartFindBar) {
        window.__kopolartFindBar.applyConfig(config);
        window.__kopolartFindBar.open();
        return;
    }

    if (!document.getElementById('kopolart-find-style')) {
        var style = document.createElement('style');
        style.id = 'kopolart-find-style';
        style.textContent = config.css || '';
        document.head.appendChild(style);
    }

    var container = document.createElement('div');
    container.id = 'kopolart-find-bar';

    var input = document.createElement('input');
    input.id = 'kopolart-find-input';
    input.type = 'text';

    var countLabel = document.createElement('span');
    countLabel.id = 'kopolart-find-count';
    countLabel.textContent = '0/0';

    var nextBtn = document.createElement('button');
    nextBtn.id = 'kopolart-find-next';
    nextBtn.type = 'button';

    var prevBtn = document.createElement('button');
    prevBtn.id = 'kopolart-find-prev';
    prevBtn.type = 'button';

    var closeBtn = document.createElement('button');
    closeBtn.id = 'kopolart-find-close';
    closeBtn.type = 'button';
    closeBtn.textContent = '✕';

    container.appendChild(input);
    container.appendChild(countLabel);
    container.appendChild(nextBtn);
    container.appendChild(prevBtn);
    container.appendChild(closeBtn);
    document.body.appendChild(container);

    function applyConfig(cfg) {
        if (!cfg) return;
        container.className = (cfg.isDark ? 'kp-dark ' : '') + (cfg.isRtl ? 'kp-rtl' : 'kp-ltr');
        if (cfg.placeholder !== undefined) input.placeholder = cfg.placeholder || '';
        if (cfg.nextText !== undefined) nextBtn.textContent = cfg.nextText || 'Next';
        if (cfg.prevText !== undefined) prevBtn.textContent = cfg.prevText || 'Previous';
    }

    applyConfig(config);

    var matches = [];
    var currentIndex = -1;

    function clearHighlights() {
        var marks = document.querySelectorAll('mark.kp-find-match');
        marks.forEach(function (mark) {
            var parent = mark.parentNode;
            if (parent) {
                parent.replaceChild(document.createTextNode(mark.textContent), mark);
                parent.normalize();
            }
        });
        matches = [];
        currentIndex = -1;
        updateCount();
    }

    function highlightText(query) {
        clearHighlights();
        if (!query || query.trim() === '') {
            return;
        }

        var cleanQuery = query.trim().toLowerCase();
        var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
            acceptNode: function (node) {
                if (!node.nodeValue || !node.nodeValue.trim()) {
                    return NodeFilter.FILTER_REJECT;
                }
                var p = node.parentElement;
                if (!p) {
                    return NodeFilter.FILTER_REJECT;
                }
                var tag = p.tagName.toLowerCase();
                if (tag === 'script' || tag === 'style' || tag === 'noscript' || tag === 'textarea' || tag === 'input' ||
                    p.id === 'kopolart-find-bar' || p.closest('#kopolart-find-bar')) {
                    return NodeFilter.FILTER_REJECT;
                }
                return NodeFilter.FILTER_ACCEPT;
            }
        });

        var nodes = [];
        while (walker.nextNode()) {
            nodes.push(walker.currentNode);
        }

        nodes.forEach(function (node) {
            var val = node.nodeValue;
            var lowerVal = val.toLowerCase();
            var index = lowerVal.indexOf(cleanQuery);
            if (index !== -1) {
                var parent = node.parentNode;
                var frag = document.createDocumentFragment();
                var lastIdx = 0;

                while (index !== -1) {
                    if (index > lastIdx) {
                        frag.appendChild(document.createTextNode(val.substring(lastIdx, index)));
                    }
                    var mark = document.createElement('mark');
                    mark.className = 'kp-find-match';
                    mark.textContent = val.substring(index, index + cleanQuery.length);
                    frag.appendChild(mark);
                    matches.push(mark);

                    lastIdx = index + cleanQuery.length;
                    index = lowerVal.indexOf(cleanQuery, lastIdx);
                }

                if (lastIdx < val.length) {
                    frag.appendChild(document.createTextNode(val.substring(lastIdx)));
                }

                if (parent) {
                    parent.replaceChild(frag, node);
                }
            }
        });

        if (matches.length > 0) {
            currentIndex = 0;
            focusMatch(currentIndex);
        } else {
            updateCount();
        }
    }

    function focusMatch(index) {
        if (matches.length === 0) {
            return;
        }
        matches.forEach(function (m) { m.classList.remove('kp-find-active'); });

        if (index >= matches.length) {
            currentIndex = 0;
        } else if (index < 0) {
            currentIndex = matches.length - 1;
        } else {
            currentIndex = index;
        }

        var activeMark = matches[currentIndex];
        if (activeMark) {
            activeMark.classList.add('kp-find-active');
            activeMark.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
        updateCount();
    }

    function updateCount() {
        if (matches.length === 0) {
            countLabel.textContent = '0/0';
        } else {
            countLabel.textContent = (currentIndex + 1) + '/' + matches.length;
        }
    }

    function open() {
        container.style.display = 'flex';
        input.focus();
        input.select();
    }

    function close() {
        clearHighlights();
        container.style.display = 'none';
    }

    function toggle() {
        if (container.style.display === 'none') {
            open();
        } else {
            close();
        }
    }

    input.addEventListener('keydown', function (e) {
        e.stopPropagation();
        if (e.key === 'Enter') {
            focusMatch(e.shiftKey ? currentIndex - 1 : currentIndex + 1);
        } else if (e.key === 'Escape') {
            close();
        }
    });

    var timer = null;
    input.addEventListener('input', function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
            highlightText(input.value);
        }, 150);
    });

    nextBtn.addEventListener('click', function () { focusMatch(currentIndex + 1); });
    prevBtn.addEventListener('click', function () { focusMatch(currentIndex - 1); });
    closeBtn.addEventListener('click', close);

    window.__kopolartFindBar = { open: open, close: close, toggle: toggle, applyConfig: applyConfig };

    input.focus();
})();
