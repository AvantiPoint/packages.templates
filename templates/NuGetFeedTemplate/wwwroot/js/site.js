// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Copy to clipboard functionality
function copyToClipboard(button) {
    const codeBlock = button.parentElement.querySelector('.code');
    const textToCopy = codeBlock.textContent || codeBlock.innerText;
    
    // Use the Clipboard API
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(textToCopy).then(function() {
            showCopyFeedback(button);
        }).catch(function(err) {
            console.error('Failed to copy text: ', err);
            fallbackCopyToClipboard(textToCopy, button);
        });
    } else {
        fallbackCopyToClipboard(textToCopy, button);
    }
}

// Fallback method for older browsers
function fallbackCopyToClipboard(text, button) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-9999px';
    textArea.style.top = '-9999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        const successful = document.execCommand('copy');
        if (successful) {
            showCopyFeedback(button);
        }
    } catch (err) {
        console.error('Fallback: Failed to copy', err);
    }
    
    document.body.removeChild(textArea);
}

// Show visual feedback when text is copied
function showCopyFeedback(button) {
    const icon = button.querySelector('.ms-Icon');
    const originalIcon = icon.className;
    
    // Change to checkmark
    icon.className = 'ms-Icon ms-Icon--CheckMark';
    button.classList.add('copied');
    button.title = 'Copied!';
    
    // Reset after 2 seconds
    setTimeout(function() {
        icon.className = originalIcon;
        button.classList.remove('copied');
        button.title = 'Copy to clipboard';
    }, 2000);
}
