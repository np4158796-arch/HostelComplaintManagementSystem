/**
 * HostelCare Client Script
 * Subject: IT5039 Web Technology with .NET
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Auto-dismiss flash alert messages after 7 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) {
                bsAlert.close();
            }
        }, 7000);
    });

    // 2. Image upload preview handler
    const imageInput = document.getElementById('imageFileInput');
    const imagePreviewContainer = document.getElementById('imagePreviewContainer');
    const imagePreview = document.getElementById('imagePreview');

    if (imageInput && imagePreview && imagePreviewContainer) {
        imageInput.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                if (file.size > 5 * 1024 * 1024) {
                    alert('File size exceeds 5MB limit. Please choose a smaller image.');
                    imageInput.value = '';
                    imagePreviewContainer.classList.add('d-none');
                    return;
                }

                const reader = new FileReader();
                reader.onload = function (event) {
                    imagePreview.src = event.target.result;
                    imagePreviewContainer.classList.remove('d-none');
                };
                reader.readAsDataURL(file);
            } else {
                imagePreviewContainer.classList.add('d-none');
            }
        });
    }

    // 3. Quick-fill credentials helper for Viva & Project Demo
    window.fillCredentials = function (email, password) {
        const emailInput = document.getElementById('loginEmail');
        const passwordInput = document.getElementById('loginPassword');
        if (emailInput && passwordInput) {
            emailInput.value = email;
            passwordInput.value = password;
            emailInput.focus();
        }
    };
});
