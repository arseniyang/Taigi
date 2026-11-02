// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// ============================================================
// УВЕДОМЛЕНИЯ (ГЛОБАЛЬНЫЕ)
// ============================================================

// ===== ФУНКЦИЯ ДЛЯ ПОКАЗА УВЕДОМЛЕНИЙ =====
function showNotification(message, type) {
    // Создаём контейнер если нет
    var container = document.getElementById('notificationContainer');
    if (!container) {
        var div = document.createElement('div');
        div.id = 'notificationContainer';
        div.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 9999; max-width: 400px; width: 100%;';
        document.body.appendChild(div);
        container = div;
    }

    // Удаляем старые уведомления
    var oldAlerts = container.querySelectorAll('.taigi-alert');
    oldAlerts.forEach(function (alert) {
        if (alert.parentNode) alert.remove();
    });

    // Создаём уведомление
    var alertDiv = document.createElement('div');
    alertDiv.className = 'taigi-alert taigi-alert-' + (type === 'error' ? 'error' : 'success');

    var icon = type === 'error' ? 'bi-exclamation-triangle' : 'bi-check-circle-fill';

    alertDiv.innerHTML = `
        <span class="taigi-alert-icon">
            <i class="bi ${icon}"></i>
        </span>
        <span class="taigi-alert-text">
            <strong>${message}</strong>
        </span>
        <button type="button" class="taigi-alert-close" aria-label="Закрыть">
            <i class="bi bi-x"></i>
        </button>
    `;

    container.appendChild(alertDiv);

    // Закрытие по клику на крестик
    alertDiv.querySelector('.taigi-alert-close').addEventListener('click', function () {
        var alert = this.closest('.taigi-alert');
        if (alert) {
            alert.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
            alert.style.opacity = '0';
            alert.style.transform = 'translateY(-10px)';
            setTimeout(function () {
                if (alert.parentNode) alert.remove();
            }, 350);
        }
    });

    // Автозакрытие через 5 секунд
    setTimeout(function () {
        if (alertDiv && alertDiv.parentNode) {
            alertDiv.style.transition = 'opacity 0.5s ease';
            alertDiv.style.opacity = '0';
            setTimeout(function () {
                if (alertDiv.parentNode) alertDiv.remove();
            }, 500);
        }
    }, 5000);
}

// ===== ЗАКРЫТИЕ УВЕДОМЛЕНИЙ ПО КЛИКУ (ДЛЯ СТАТИЧЕСКИХ) =====
document.addEventListener('DOMContentLoaded', function () {
    // Закрытие по клику на крестик
    document.querySelectorAll('.taigi-alert-close').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var alert = this.closest('.taigi-alert');
            if (alert) {
                alert.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                alert.style.opacity = '0';
                alert.style.transform = 'translateY(-10px)';
                setTimeout(function () {
                    if (alert.parentNode) alert.remove();
                }, 350);
            }
        });
    });

    // Автозакрытие статичеких уведомлений через 5 секунд
    document.querySelectorAll('.taigi-alert').forEach(function (alert) {
        setTimeout(function () {
            if (alert && alert.parentNode) {
                alert.style.transition = 'opacity 0.5s ease';
                alert.style.opacity = '0';
                setTimeout(function () {
                    if (alert.parentNode) alert.remove();
                }, 500);
            }
        }, 5000);
    });
});





// ============================================================
// КАСТОМНЫЙ CONFIRM (заменяет браузерный)
// ============================================================
// ============================================================
// КАСТОМНЫЙ CONFIRM (В СТИЛЕ TAIGI)
// ============================================================
function showConfirm(title, message, callback) {
    // Удаляем старую модалку
    $('#customConfirmModal').remove();

    var modal = `
        <div class="modal fade" id="customConfirmModal" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
            <div class="modal-dialog modal-dialog-centered" style="max-width: 440px;">
                <div class="modal-content" style="border-radius: 0; border: 1px solid #1a1a1a; background: #ffffff; box-shadow: 0 8px 40px rgba(0,0,0,0.12);">
                    
                    <!-- Шапка -->
                    <div class="modal-header border-0" style="padding: 28px 28px 0 28px;">
                        <h5 class="modal-title fw-bold" style="font-size: 18px; letter-spacing: 1px; color: #1a1a1a;">
                            ${title}
                        </h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Закрыть" style="opacity: 1; color: #1a1a1a;"></button>
                    </div>
                    
                    <!-- Тело -->
                    <div class="modal-body" style="padding: 16px 28px 8px 28px;">
                        <p style="font-size: 15px; color: #555; line-height: 1.6; margin: 0;">
                            ${message}
                        </p>
                    </div>
                    
                    <!-- Футер -->
                    <div class="modal-footer border-0" style="padding: 16px 28px 28px 28px; gap: 12px;">
                        <button type="button" class="btn btn-outline-dark" data-bs-dismiss="modal" 
                                style="border-radius: 0; padding: 10px 32px; font-size: 14px; font-weight: 500; letter-spacing: 0.5px; border-color: #1a1a1a; color: #1a1a1a; background: transparent; transition: all 0.2s;">
                            Нет
                        </button>
                        <button type="button" class="btn btn-dark" id="confirmYes" 
                                style="border-radius: 0; padding: 10px 32px; font-size: 14px; font-weight: 500; letter-spacing: 0.5px; background: #1a1a1a; color: #ffffff; border: 1px solid #1a1a1a; transition: all 0.2s;">
                            Да
                        </button>
                    </div>
                    
                </div>
            </div>
        </div>
    `;

    $('body').append(modal);

    var modalElement = $('#customConfirmModal');
    modalElement.modal('show');

    // Обработчик для кнопки "Да"
    $('#confirmYes').off('click').on('click', function () {
        modalElement.modal('hide');
        modalElement.on('hidden.bs.modal', function () {
            modalElement.remove();
        });
        callback();
    });

    // При закрытии через крестик или "Нет" — удаляем модалку
    modalElement.on('hidden.bs.modal', function () {
        if ($('#customConfirmModal').length) {
            $('#customConfirmModal').remove();
        }
    });
}

// ============================================================
// ПРОМОКОДЫ В КОРЗИНЕ (AJAX)
// ============================================================
$(document).ready(function () {

    // ===== ПРИМЕНИТЬ ПРОМОКОД (через делегирование) =====
    $(document).on('click', '#applyPromoBtn', function (e) {
        e.preventDefault();

        var promoCode = $('#promoCodeInput').val().trim();
        if (!promoCode) {
            showNotification('Введите код промокода', 'error');
            return;
        }

        var btn = $(this);
        btn.prop('disabled', true).text('Применяется...');

        $.ajax({
            url: '/Cart/ApplyPromoCode',
            type: 'POST',
            data: { promoCode: promoCode },
            success: function (response) {
                if (response.success) {
                    showNotification(response.message, 'success');
                    updatePromoUI(response);
                } else {
                    showNotification(response.message, 'error');
                }
            },
            error: function () {
                showNotification('Ошибка при применении промокода', 'error');
            },
            complete: function () {
                btn.prop('disabled', false).text('Применить');
                $('#promoCodeInput').val('');
            }
        });
    });

    // ===== УДАЛИТЬ ПРОМОКОД =====
    $(document).on('click', '#removePromoBtn', function (e) {
        e.preventDefault();

        $.ajax({
            url: '/Cart/RemovePromoCode',
            type: 'POST',
            success: function (response) {
                if (response.success) {
                    showNotification(response.message, 'success');
                    removePromoUI(response);
                } else {
                    showNotification(response.message, 'error');
                }
            },
            error: function () {
                showNotification('Ошибка при удалении промокода', 'error');
            }
        });
    });

});

// ============================================================
// ОБНОВЛЕНИЕ UI ПРИ ПРИМЕНЕНИИ ПРОМОКОДА
// ============================================================
function updatePromoUI(response) {
    // 1. Скрываем форму ввода
    $('#promoFormBlock').hide();

    // 2. Удаляем старый блок обычного итога, если он есть
    var oldTotalBlock = $('#cartTotal').closest('.cart-summary-total');
    if (oldTotalBlock.length) oldTotalBlock.remove();

    // 3. Удаляем предыдущий блок со скидкой (если был) — для предотвращения дублей
    $('#promoAppliedBlock').remove();
    var oldDiscountRow = $('#discountAmount').closest('.cart-summary-row');
    if (oldDiscountRow.length) oldDiscountRow.remove();
    var oldFinalBlock = $('#cartFinalTotal').closest('.cart-summary-total');
    if (oldFinalBlock.length) oldFinalBlock.remove();

    // 4. Вставляем новый блок с применённым промокодом
    var promoHtml = `
        <div class="cart-promo-applied" id="promoAppliedBlock">
            <span>Промокод <strong>${response.promoCode}</strong> <span class="text-muted small">(${response.discountPercent}%)</span></span>
            <button type="button" class="cart-promo-remove" id="removePromoBtn">✕</button>
        </div>
        <div class="cart-summary-row">
            <span>Скидка</span>
            <span class="text-danger" id="discountAmount">-${response.discountAmount.toLocaleString()} ₽</span>
        </div>
        <hr class="cart-divider" />
        <div class="cart-summary-total">
            <span>Итого со скидкой</span>
            <span class="fw-bold text-success" id="cartFinalTotal">${response.finalTotal.toLocaleString()} ₽</span>
        </div>
    `;

    // Вставляем после существующего hr (первого разделителя)
    var firstDivider = $('.cart-divider').first();
    if (firstDivider.length) {
        firstDivider.after(promoHtml);
    } else {
        // fallback
        $('.cart-summary').append(promoHtml);
    }

    // 5. Обновляем обычную сумму товаров (без скидки)
    if ($('#cartSubtotal').length) {
        $('#cartSubtotal').text(response.total.toLocaleString() + ' ₽');
    }

    // 6. Переключаем обработчик для кнопки удаления
    $('#removePromoBtn').on('click', function () {
        removePromo();
    });
}

// ============================================================
// ОБНОВЛЕНИЕ UI ПРИ УДАЛЕНИИ ПРОМОКОДА
// ============================================================
function removePromoUI(response) {
    // 1. Удаляем блок с применённым промокодом
    $('#promoAppliedBlock').remove();

    // 2. Удаляем строку со скидкой
    var discountRow = $('#discountAmount').closest('.cart-summary-row');
    if (discountRow.length) discountRow.remove();

    // 3. Удаляем разделитель, который был перед итогом со скидкой (второй hr)
    var dividers = $('.cart-divider');
    if (dividers.length > 1) {
        // Оставляем первый hr (после доставки), удаляем второй
        $(dividers[1]).remove();
    }

    // 4. Удаляем блок с итогом со скидкой
    var finalTotalBlock = $('#cartFinalTotal').closest('.cart-summary-total');
    if (finalTotalBlock.length) finalTotalBlock.remove();

    // 5. Проверяем, есть ли уже блок с обычным итогом
    if ($('#cartTotal').length) {
        // Если есть — просто обновляем текст и показываем, если был скрыт
        $('#cartTotal').text(response.total.toLocaleString() + ' ₽');
        $('#cartTotal').closest('.cart-summary-total').show();
    } else {
        // Если нет — создаём заново
        var lastRow = $('.cart-summary-row').last();
        var promoFormHtml = `
            <div class="cart-promo" id="promoFormBlock">
                <div class="cart-promo-input-group">
                    <input type="text" class="cart-promo-input" placeholder="Промокод" id="promoCodeInput" autocomplete="off" />
                    <button type="button" class="cart-promo-btn" id="applyPromoBtn">Применить</button>
                </div>
            </div>
            <hr class="cart-divider" />
            <div class="cart-summary-total">
                <span>Итого</span>
                <span class="fw-bold" id="cartTotal">${response.total.toLocaleString()} ₽</span>
            </div>
        `;
        if (lastRow.length) {
            lastRow.after(promoFormHtml);
        } else {
            $('.cart-summary').append(promoFormHtml);
        }
    }

    // 6. Обновляем сумму товаров
    if ($('#cartSubtotal').length) {
        $('#cartSubtotal').text(response.total.toLocaleString() + ' ₽');
    }

// 7. Убеждаемся, что форма ввода видна
    $('#promoFormBlock').show();
}



// ============================================================
// КОРЗИНА: ОПЕРАЦИИ С ТОВАРАМИ (AJAX)
// ============================================================

$(document).ready(function () {

    // ===== ОБНОВЛЕНИЕ ИТОГОВ =====
    window.updateTotals = function (response) {
        if ($('#cartTotal').length) {
            $('#cartTotal').text(response.total.toLocaleString() + ' ₽');
        }
        if ($('#cartFinalTotal').length) {
            $('#cartFinalTotal').text(response.finalTotal.toLocaleString() + ' ₽');
        }
        if ($('#cartSubtotal').length) {
            $('#cartSubtotal').text(response.total.toLocaleString() + ' ₽');
        }
        if ($('#cartCount').length) {
            $('#cartCount').text(response.count);
        }
        if ($('#cartItemsCount').length) {
            var countText = response.count + ' ' + getDeclension(response.count);
            $('#cartItemsCount').text(countText);
        }
        if ($('#discountAmount').length && response.discountAmount !== undefined) {
            $('#discountAmount').text('-' + response.discountAmount.toLocaleString() + ' ₽');
        }
    };

    // ===== СКЛОНЕНИЕ СЛОВ =====
    window.getDeclension = function (count) {
        if (count % 10 === 1 && count % 100 !== 11) return 'товар';
        if (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20)) return 'товара';
        return 'товаров';
    };

    // ===== УВЕЛИЧИТЬ =====
    window.increase = function (productId, size) {
        var elementId = productId + '-' + size;
        $.ajax({
            url: '/Cart/Increase',
            type: 'POST',
            data: { productId: productId, size: size },
            success: function (response) {
                if (response.success) {
                    $('#qty-' + elementId).text(response.newQuantity);
                    $('#subtotal-' + elementId).text(response.itemTotal.toLocaleString() + ' ₽');
                    updateTotals(response);
                } else {
                    showNotification(response.message, 'error');
                }
            },
            error: function () {
                showNotification('Ошибка при обновлении количества', 'error');
            }
        });
    };

    // ===== УМЕНЬШИТЬ =====
    window.decrease = function (productId, size) {
        var elementId = productId + '-' + size;
        $.ajax({
            url: '/Cart/Decrease',
            type: 'POST',
            data: { productId: productId, size: size },
            success: function (response) {
                if (response.success) {
                    if (response.removed) {
                        $('#cart-item-' + elementId).fadeOut(300, function () {
                            $(this).remove();
                            if ($('.cart-item').length === 0) {
                                location.reload();
                            }
                        });
                    } else {
                        $('#qty-' + elementId).text(response.newQuantity);
                        $('#subtotal-' + elementId).text(response.itemTotal.toLocaleString() + ' ₽');
                    }
                    updateTotals(response);
                } else {
                    showNotification(response.message, 'error');
                }
            },
            error: function () {
                showNotification('Ошибка при обновлении количества', 'error');
            }
        });
    };

    // ===== УДАЛИТЬ =====
    window.removeItem = function (productId, size) {
        showConfirm(
            'Удалить товар из корзины?',
            'Вы уверены, что хотите удалить товар из корзины?',
            function () {
                // Это выполняется, если пользователь нажал "Да"
                var elementId = productId + '-' + size;
                $.ajax({
                    url: '/Cart/RemoveFromCart',
                    type: 'POST',
                    data: { productId: productId, size: size },
                    success: function (response) {
                        if (response.success) {
                            $('#cart-item-' + elementId).fadeOut(300, function () {
                                $(this).remove();
                                if ($('.cart-item').length === 0) {
                                    location.reload();
                                }
                            });
                            updateTotals(response);
                        } else {
                            showNotification(response.message, 'error');
                        }
                    },
                    error: function () {
                        showNotification('Ошибка при удалении товара', 'error');
                    }
                });
            }
        );
    };

});


                                    // ============================================================
// ЛОКАЛИЗАЦИЯ КЛИЕНТСКОЙ ВАЛИДАЦИИ (jQuery Validation)
// ============================================================
$.extend($.validator.messages, {
    required: "Это поле обязательно для заполнения.",
    remote: "Пожалуйста, исправьте это поле.",
    email: "Пожалуйста, введите корректный email.",
    url: "Пожалуйста, введите корректный URL.",
    date: "Пожалуйста, введите корректную дату.",
    dateISO: "Пожалуйста, введите корректную дату (ISO).",
    number: "Пожалуйста, введите корректное число.",
    digits: "Пожалуйста, вводите только цифры.",
    creditcard: "Пожалуйста, введите корректный номер карты.",
    equalTo: "Пожалуйста, введите такое же значение еще раз.",
    maxlength: $.validator.format("Пожалуйста, введите не больше {0} символов."),
    minlength: $.validator.format("Пожалуйста, введите не меньше {0} символов."),
    rangelength: $.validator.format("Пожалуйста, введите значение длиной от {0} до {1} символов."),
    range: $.validator.format("Пожалуйста, введите число от {0} до {1}."),
    max: $.validator.format("Пожалуйста, введите число, меньшее или равное {0}."),
    min: $.validator.format("Пожалуйста, введите число, большее или равное {0}.")
});



    // ============================================================
// ПЕРЕВОД СТАНДАРТНЫХ ОШИБОК UNOBTRUSIVE (The field is required)
// ============================================================
$(document).ready(function () {
    // Переопределяем стандартный текст, который генерирует сервер
    if ($.validator && $.validator.unobtrusive && $.validator.unobtrusive.adapters) {
        
        // Перехватываем стандартные сообщения
        $.extend($.validator.messages, {
            required: "Это поле обязательно для заполнения.",
            email: "Введите корректный email.",
            number: "Введите число.",
            date: "Введите корректную дату."
        });

        // Заставляем unobtrusive использовать наши переводы
        $.validator.unobtrusive.parseElement = (function (orig) {
            return function (element, skipAttach) {
                var result = orig.call(this, element, skipAttach);
                $(element).find('[data-val-required]').each(function () {
                    // Заменяем английский текст на русский
                    $(this).attr('data-val-required', 'Это поле обязательно для заполнения.');
                });
                $(element).find('[data-val-email]').each(function () {
                    $(this).attr('data-val-email', 'Введите корректный email.');
                });
                return result;
            };
        })($.validator.unobtrusive.parseElement);
    }
});