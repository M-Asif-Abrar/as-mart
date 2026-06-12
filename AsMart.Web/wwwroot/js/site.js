// site.js - global scripts for As-Mart

(function () {
    function onReady(callback) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', callback);
        } else {
            callback();
        }
    }

    function safeGtag(eventName, params) {
        if (typeof window.gtag === 'function') {
            window.gtag('event', eventName, params || {});
        }
    }

    onReady(function () {

        // ----------------------------
        // Affiliate clicks → affiliate_click
        // ----------------------------
        document.querySelectorAll('.js-affiliate-click').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var pid = this.dataset.productId;
                var title = this.dataset.productTitle;
                var category = this.dataset.category;
                var priceStr = this.dataset.price;
                var price = priceStr ? parseFloat(priceStr) : 0;

                safeGtag('affiliate_click', {
                    event_category: 'ecommerce',
                    event_label: title,
                    value: price,
                    items: [{
                        item_id: pid,
                        item_name: title,
                        item_category: category,
                        price: price
                    }]
                });
            });
        });

        // ----------------------------
        // Add to wishlist → add_to_wishlist
        // ----------------------------
        document.querySelectorAll('.js-add-wishlist').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var pid = this.dataset.productId;
                var title = this.dataset.productTitle;

                safeGtag('add_to_wishlist', {
                    event_category: 'engagement',
                    event_label: title,
                    items: [{
                        item_id: pid,
                        item_name: title
                    }]
                });
            });
        });

        // ----------------------------
        // Mark as purchased → purchase_marked
        // ----------------------------
        document.querySelectorAll('.js-mark-purchased').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var pid = this.dataset.productId;
                var title = this.dataset.productTitle;

                safeGtag('purchase_marked', {
                    event_category: 'engagement',
                    event_label: title,
                    items: [{
                        item_id: pid,
                        item_name: title
                    }]
                });
            });
        });

        // Optional: “view from wishlist” and “view from purchases”
        document.querySelectorAll('.js-view-from-wishlist').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var pid = this.dataset.productId;
                var title = this.dataset.productTitle;

                safeGtag('view_from_wishlist', {
                    event_category: 'engagement',
                    event_label: title,
                    items: [{
                        item_id: pid,
                        item_name: title
                    }]
                });
            });
        });

        document.querySelectorAll('.js-view-from-purchases').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var pid = this.dataset.productId;
                var title = this.dataset.productTitle;

                safeGtag('view_from_purchases', {
                    event_category: 'engagement',
                    event_label: title,
                    items: [{
                        item_id: pid,
                        item_name: title
                    }]
                });
            });
        });
    });
})();
