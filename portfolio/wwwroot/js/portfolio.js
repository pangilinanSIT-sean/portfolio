// The browser side of the picture-address rule. The server checks the same rule again with Regex.IsMatch.
(function () {
    var imageRule = /^https?:\/\/\S+\.(png|jpe?g|gif|webp|PNG|JPE?G|GIF|WEBP)(\?\S*)?$/;

    document.querySelectorAll('form[data-image-lines]').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            var box = form.querySelector('[data-image-lines-box]');
            var message = form.querySelector('[data-image-lines-msg]');
            if (!box || !message) { return; }

            // Check the box line by line, skipping blank lines.
            var lines = box.value.split(/\r?\n/);
            for (var i = 0; i < lines.length; i++) {
                var line = lines[i].trim();
                if (line !== '' && !imageRule.test(line)) {
                    event.preventDefault();
                    message.textContent = 'Line ' + (i + 1) + ' is not a picture address. It must start with http and end in .png, .jpg, .gif or .webp.';
                    return;
                }
            }
            message.textContent = '';
        });
    });
})();
