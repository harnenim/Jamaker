$(document).on("keydown", function(e) {
	switch (e.keyCode) {
		case 27: { // Esc
			requestClose();
			break;
		}
	}
});

window._close = window.close;
window.close = () => {
	if (windowName != "finder") {
		(opener ? opener.binder : binder).focus("editor");
	}
	if (opener) {
		window._close();
	}
};

// 종료 전 확인 필요한 경우 override
function requestClose() {
	window.close();
}

function confirmCancel() {
	confirm("작업을 취소하시겠습니까?"
	,	() => {
			window.close();
		}
	);
}

// 메인 폼에서 보내주는 메시지, 경우에 따라 override
function sendMsg(msg) {
	alert(msg);
}

window.windowName = null;

// alert 재정의
window._alert = alert;
alert = (msg) => {
	if (windowName && opener && opener.binder) {
		opener.binder.alert(windowName, msg);
	} else if (windowName && window.binder) {
		binder.alert(windowName, msg);
	} else {
		_alert(msg);
	}
}
// confirm 재정의
window._confirm = confirm;
window.afterConfirmYes = () => {};
window.afterConfirmNo  = () => {};
confirm = (msg, yes, no) => {
	if (windowName) {
		if (opener) {
			opener.afterConfirmYes = yes ? yes : () => {};
			opener.afterConfirmNo  = no  ? no  : () => {};
			opener.binder.confirm(windowName, msg);
		} else {
			afterConfirmYes = yes ? yes : () => {};
			afterConfirmNo  = no  ? no  : () => {};
			if (window.binder) {
				binder.confirm(windowName, msg);
			} else {
				if (_confirm(msg)) {
					afterConfirmYes();
				} else {
					afterConfirmNo();
				}
			}
		}
	} else {
		const result = _confirm(msg);
		if (result) {
			if (yes) yes();
		} else {
			if (no) no();
		}
		return result;
	}
}

// opener가 있는 addon에서만 쓰임
window.loadAddonSetting = null;
window.saveAddonSetting = null;
if (opener) {
	opener.afterLoadAddonSetting = () => {};
	loadAddonSetting = (name, afterLoad) => {
		opener.afterLoadAddonSetting = afterLoad ? afterLoad : () => {};
		opener.binder.loadAddonSetting(name);
	}
	
	opener.afterSaveAddonSetting = () => {};
	saveAddonSetting = (name, text, afterSave) => {
		opener.afterSaveAddonSetting = afterSave ? afterSave : () => {};
		opener.binder.saveAddonSetting(name, text);
	}
} else {
	// 프레임 샘플에선 opener가 나중에 추가됨
	$(() => {
		if (opener) {
			opener.afterLoadAddonSetting = () => {};
			loadAddonSetting = (name, afterLoad) => {
				opener.afterLoadAddonSetting = afterLoad ? afterLoad : () => {};
				opener.binder.loadAddonSetting(name);
			}
			
			opener.afterSaveAddonSetting = () => {};
			saveAddonSetting = (name, text, afterSave) => {
				opener.afterSaveAddonSetting = afterSave ? afterSave : () => {};
				opener.binder.saveAddonSetting(name, text);
			}
		}
	});
}

function setColor(color) {
	$.ajax({url: "lib/popup.color.css"
		,	dataType: "text"
		,	success: (preset) => {
				for (let name in color) {
					preset = preset.split("[" + name + "]").join(color[name]);
				}
				
				let $style = $("#styleColor");
				if (!$style.length) {
					$("head").append($style = $("<style id='styleColor'>"));
				}
				$style.html(preset);
			}
	});
}

$(() => {
	$("textarea").attr({ spellcheck: false });
});
