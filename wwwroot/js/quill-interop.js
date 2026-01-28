window.quillInterop = {
    initialize: function (editorElement, toolbarElement, initialContent, dotNetHelper) {
        var quill = new Quill(editorElement, {
            modules: {
                toolbar: toolbarElement
            },
            theme: 'snow'
        });

        if (initialContent) {
            quill.root.innerHTML = initialContent;
        }

        quill.on('text-change', function () {
            dotNetHelper.invokeMethodAsync('OnContentChanged', quill.root.innerHTML);
        });

        return quill;
    },
    getHtml: function (quill) {
        return quill.root.innerHTML;
    },
    setHtml: function (quill, html) {
        quill.root.innerHTML = html;
    }
};
