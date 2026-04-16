import "./Subtitle.Converter.js?260415";
import "./jszip.min.js";
import "./WinPNG.js";

if (!Uint8Array.fromBase64) {
	// 삼성브라우저 같은 경우 아직 base64 관련 함수 미지원
	Uint8Array.fromBase64 = (base64) => {
		return Uint8Array.from(atob(base64), c => c.charCodeAt(0));
	};
}

URL.files = {};
URL.from = function(blob) {
	const url = URL.createObjectURL(blob);
	this.files[url] = blob;
	return url;
}
URL.clear = function(required) {
	for (let url in this.files) {
		if (url == required) continue;
		URL.revokeObjectURL(url);
		delete this.files[url];
	}
}
URL.reset = function(blob) {
	this.clear();
	return this.from(blob);
}

let winPNG;
let inputUrl;
let ivTarget;
let cbJamaker;
let areaSettingZip;
let viewFileList;
let previewImg;
let popup;

const input = new Image();
input.crossorigin = "anonymous";
const canvas = document.createElement("canvas");
input.onload = async function() {
	try {
		winPNG.classList.add("progress");
		
        areaSettingZip.style.display = "none";
		viewFileList.innerHTML = "";
		viewFileList.style.minWidth = "";
		
		const bmp = new BufferedImage(this);
		const possibility = WithTarget.possibility(bmp);
		let parsed = await WithTarget.fromBitmap(bmp, possibility);
		
		if (!parsed) {
			// 키값 입력받아서 재시도
			const key = prompt("이미지를 해석할 수 없습니다.\n비밀번호가 있다면 키를 입력하세요.");
			parsed = await WithTarget.fromBitmap(bmp, possibility, getShift(key), getXors(key));
			
			if (!parsed) {
				showTargetImage(bmp);
				setTimeout(() => {
					alert("해석 실패");
				}, 100);
				files = oldFiles;
				return;
			}
		}
		
		previewImg.style.display = "";
		showTargetImage(parsed.targetImage);
		winPNG.classList.add("open");
		
		parsed.containers.sort((cont1, cont2) => {
			return compare(cont1.path, cont2.path);
		});
		for (let cont of parsed.containers) {
			await addFile(cont);
		}
		
		resizeViewFileList();
		
	} catch (e) {
		console.error(e);
		if (e.message.indexOf("cross-origin") > 0) {
			alert("외부 URL은 열 수 없습니다.");
		}
	}
	winPNG.classList.remove("progress");
}
input.onerror = function(err) {
	alert("열지 못했습니다.");
}
async function unzip(zipFile) {
	let result = false;
	try {
		winPNG.classList.add("progress");
		
		viewFileList.innerHTML = "";
		viewFileList.style.minWidth = "";
		
		const zip = await JSZip.loadAsync(zipFile);
		for (key in zip.files) {
			const file = zip.files[key];
			if (file.dir) continue;
			
			await addFile({ path: file.name, binary: await file.async("uint8array") });
		}
		
		resizeViewFileList();
		
		winPNG.classList.add("open");
		result = true;
		
	} catch (e) {
		console.error(e);
	}
	winPNG.classList.remove("progress");
	return result;
}
async function setOne(file, filename=null) {
	try {
		winPNG.classList.add("progress");
		
		viewFileList.innerHTML = "";
		viewFileList.style.minWidth = "";
		
		await addFile({ path: filename ? filename : (file.name ? file.name : "unknown"), binary: new Uint8Array(await file.arrayBuffer()) });
		
		resizeViewFileList();
		
		winPNG.classList.add("open");
		
	} catch (e) {
		console.error(e);
	}
	winPNG.classList.remove("progress");
}
function resizeViewFileList() {
	let minWidth = 0;
	[...viewFileList.children].forEach((a) => {
		let aWidth = 3;
		a.childNodes.forEach((span) => {
			aWidth += span.getBoundingClientRect().width;
		});
		minWidth = Math.max(minWidth, aWidth);
	});
	viewFileList.style.minWidth = minWidth + "px";
}
function compare(path1, path2) {
	const index1 = path1.indexOf("/");
	const index2 = path2.indexOf("/");
	if (index1 < 0) {
		if (index2 < 0) {
			// 파일끼리 대소문자 무시하고 비교
			return (path1.toUpperCase() < path2.toUpperCase()) ? -1 : 1;
		} else {
			// Java 버전과 달리 폴더보다 파일이 먼저 보이게 함
			return -1;
		}
	} else {
		if (index2 < 0) {
			// Java 버전과 달리 폴더보다 파일이 먼저 보이게 함
			return 1;
		} else {
			const dir1 = path1.substring(0, index1);
			const dir2 = path2.substring(0, index2);
			if (dir1 == dir2) {
				// 같은 폴더면 하위 내용물 비교
				return compare(path1.substring(index1 + 1), path2.substring(index2 + 1));
			} else {
				// 폴더끼리 대소문자 무시하고 비교
				return (dir1.toUpperCase() < dir2.toUpperCase()) ? -1 : 1;
			}
		}
	}
}
async function addFile(cont) {
	const path = cont.path.split("/");
	const filename = path[path.length - 1];
	
	const a = document.createElement("a");
	
	const labelDir = document.createElement("span");
	labelDir.innerText = cont.path.substring(0, cont.path.lastIndexOf('/') + 1);
	
	const labelFile = document.createElement("span");
	labelFile.className = "file";
	const file = new File([cont.binary], filename);
	a.setAttribute("data-href", a.href = URL.from(file));
	a.download = labelFile.innerText = filename;
	
	a.append(labelDir);
	a.append(labelFile);
	viewFileList.append(a);
	
	let type = "";
	const extIndex = filename.lastIndexOf(".") + 1;
	const ext = filename.substring(extIndex).toLowerCase();
	switch (ext) {
		case "png":
		case "bmp":
		case "gif":
		case "jpg":
		case "jpeg":
		case "exif":
		case "tiff":
		case "ico":
		case "cur":
		case "svg":
		case "svgz":
		case "webp":
			a.className = (type = "image");
			break;
		case "smi":
		case "sami": {
			// Jamaker 주석 제거 기능
			let text = await file.text();
			text = text.split("\n</SAMI>")[0] + "\n</SAMI>";
			{	const parts = text.split("\n<!-- End=");
				for (let i = 1; i < parts.length; i++) {
					parts[i] = parts[i].split("-->").slice(1).join("-->");
				}
				text = parts.join("");
			}
			{	const parts = text.split("\n<!-- ASS");
				for (let i = 1; i < parts.length; i++) {
					parts[i] = parts[i].split("-->").slice(1).join("-->");
				}
				text = parts.join("");
			}
			const clearedFile = new File([new Blob(["\uFEFF" + text], { type: "text/plain;charset=utf-8" })], filename);
			const clearedUrl = URL.from(clearedFile);
			a.setAttribute("data-cleared", clearedUrl);
			
			if (cbJamaker.checked) {
				a.href = clearedUrl;
			}
		}
		case "jmk": {
			// jmk/smi -> smi/ass/srt 변환 기능
			const holds = SmiFile.textToHolds(await file.text());
			if (holds[0].fs) {
				const match = /<sami( [^>]*)*>/gi.exec(holds[0].text);
				
				let withSmi = false;
				let withSrt = false;
				let withAss = false;
				
				if (match && match[1]) {
					const attrs = match[1].toUpperCase().split(" ");
					attrs.forEach((attr) => {
						if (attr == "SMI") {
							withSmi = true;
						} else if (attr == "SRT") {
							withSrt = true;
						} else if (attr == "ASS") {
							withAss = true;
						}
					});
				}

				Subtitle.video.fs  = holds[0].fs;
				Subtitle.video.kfs = holds[0].kfs ?? [];
				
				if (withSmi) {
					const smiFilename = filename.substring(0, extIndex) + "smi";
					const smiText = SmiFile.holdsToText(holds, true, true, -1);
					const smiFile = new File([new Blob(["\uFEFF" + smiText], { type: "text/plain;charset=utf-8" })], smiFilename);
					const smiUrl = URL.from(smiFile);
					a.setAttribute("data-smi", smiUrl);
					
					const smiA = document.createElement("a");
					smiA.href = smiUrl;
					smiA.download = smiFilename;
					smiA.innerText = "[SMI] ";
					labelFile.before(smiA);
				}
				if (withAss) {
					const append = new AssFile(holds[0].ass ?? "");
					const appendParts = [];
					append.parts.forEach((part) => {
						switch (part.name) {
							case "Script Info":
							case "V4+ Styles":
							case "Events":
								break;
							default: {
								appendParts.push(part);
							}
						}
					});
					let x = 1920;
					let y = 1080;
					const info = append.getInfo();
					if (info) {
						let playResX = info.get("PlayResX");
						let playResY = info.get("PlayResY");
						if (playResX && playResY) {
							x = playResX;
							y = playResY;
						}
					}
					holds.forEach((hold) => {
						hold.smiFile = new SmiFile(hold.text);
					});
					const assFilename = filename.substring(0, extIndex) + "ass";
					const assText = SmiFile.holdsToAss(holds, appendParts, append.getStyles().body, append.getEvents().body, x, y).toText();
					const assFile = new File([new Blob(["\uFEFF" + assText], { type: "text/plain;charset=utf-8" })], assFilename);
					const assUrl = URL.from(assFile);
					a.setAttribute("data-ass", assUrl);
					
					const assA = document.createElement("a");
					assA.href = assUrl;
					assA.download = assFilename;
					assA.innerText = "[ASS] ";
					labelFile.before(assA);
				}
				if (ext == "jmk" && (withSmi || withAss)) {
					// JMK 원본 다운로드는 ZIP 다운로드에서만 제공
					a.removeAttribute("href");
				}
				if (withSmi || withAss) {
					areaSettingZip.style.display = "block";
				}
			}
		}
		case "srt":
		case "ass":
		case "txt":
		case "xml":
		case "html":
		case "js":
		case "css":
		case "ts":
		case "json":
		case "java":
		case "cs":
		case "cpp":
		case "jsp":
		case "asp":
		case "php":
		case "bat":
		case "sh":
		case "log":
			a.className = (type = "text");
			break;
	}
	
	if (type) {
		const labelPreview = document.createElement("span");
		labelPreview.className = "preview";
		labelPreview.innerText = "[미리보기]";
		a.append(labelPreview);
	}
}
function showTargetImage(bmp) {
	ivTarget.innerHTML = "";
	if (bmp == null) {
		const comment = document.createElement("div");
		comment.id = "comment";
		comment.append("입력 이미지 없이 생성한");
		comment.append(document.createElement("br"));
		comment.append("이미지입니다.");
		ivTarget.append(comment);
		return;
	}
	ivTarget.append(bmp.update().canvas);
	let height = ivTarget.clientHeight;
	let ratio = height / bmp.height;
	let width = bmp.width * ratio;
	if (width > ivTarget.clientWidth) {
		width = ivTarget.clientWidth;
		ratio = width / bmp.width;
		height = bmp.height * ratio;
	}
	bmp.canvas.style.width = width + "px";
	bmp.canvas.style.height = height + "px";
	bmp.canvas.style.margin = (-height / 2) + "px " + (-width / 2) + "px";
}
window.downloadZip = function() {
	const as = [...document.getElementById("viewFileList").children];
	if (!as.length) {
		alert("PNG 파일을 열지 않았습니다.");
		return;
	}
	
	const priority = winPNG.querySelector("form").priority.value;
	
	// jszip timezone 보정 안 됨
	const now = new Date();
	now.setTime(now.getTime() - (now.getTimezoneOffset() * 60000));
	const opt = { date: now };
	
	const zip = new JSZip();
	as.forEach((a) => {
		const spans = a.children;
		let name = a.querySelector("span").innerText + a.querySelector("span.file").innerText;
		
		let completed = false;
		const extIndex = name.lastIndexOf(".") + 1;
		const ext = name.substring(extIndex).toLowerCase();
		if (ext == "jmk") {
			if (priority == "smi" || priority == "all") {
				const href = a.getAttribute("data-smi");
				if (href) {
					// 우선순위 SMI 변환 존재
					zip.file(name.substring(0, extIndex) + "smi", URL.files[href], opt);
					completed = (priority == "smi");
				} else if (priority == "smi") {
					// 우선순위 SMI 변환 없는데 ASS 변환 있으면 대체재로 넣음
					const href = a.getAttribute("data-ass");
					if (href) {
						zip.file(name.substring(0, extIndex) + "ass", URL.files[href], opt);
						completed = true;
					}
				}
			}
			if (!completed && (priority == "ass" || priority == "all")) {
				const href = a.getAttribute("data-ass");
				if (href) {
					// 우선순위 ASS 변환 존재
					zip.file(name.substring(0, extIndex) + "ass", URL.files[href], opt);
					completed = (priority == "ass");
				} else if (priority == "ass") {
					// 우선순위 ASS 변환 없는데 SMI 변환 있으면 대체재로 넣음
					const href = a.getAttribute("data-smi");
					if (href) {
						zip.file(name.substring(0, extIndex) + "smi", URL.files[href], opt);
						completed = true;
					}
				}
			}
		} else if (ext == "smi") {
			if (priority == "ass" || priority == "all") {
				const href = a.getAttribute("data-ass");
				if (href) {
					// 우선순위 ASS 변환 존재
					zip.file(name.substring(0, extIndex) + "ass", URL.files[href], opt);
					completed = (priority == "ass");
				}
			}
		}
		if (!completed) {
			let href = a.href;
			if (!href) href = a.getAttribute("data-href");
			if (href) zip.file(name, URL.files[href], opt);
		}
	});
	zip.generateAsync({ type: "blob" }).then((blob) => {
		const a = document.createElement("a");
		a.href = URL.createObjectURL(blob);
		a.download = "WinPNG_" + new Date().getTime() + ".zip";
		a.click();
		a.remove();
	});
}

window.parse = function() {
	try {
		const url = inputUrl.value;
		URL.clear(url.startsWith("blob:") ? url : null);
		input.src = url;
	} catch (e) {
		console.debug(e);
	}
	return false;
}
async function paste(e) {
	if (e.clipboardData.files.length) {
		e.preventDefault();
		inputUrl.value = input.src = URL.reset(e.clipboardData.files[0]);
	} else {
		inputUrl.value = "";
		inputUrl.focus();
	}
}

async function onload() {
	inputUrl = document.getElementById("inputUrl");
	ivTarget = document.getElementById("ivTarget");
	areaSettingZip = document.getElementById("areaSettingZip");
	viewFileList = document.getElementById("viewFileList");
	previewImg = document.getElementById("previewImg");
	
	document.getElementById("toggleWinPNG").addEventListener("click", () => {
		if (winPNG.classList.contains("on")) {
			winPNG.classList.remove("on");
			winPNG.classList.remove("open");
		} else {
			winPNG.classList.add("on");
		}
	});
	
	// 포커스 상관없이 붙여넣기 이벤트 동작
	document.addEventListener("paste", paste);
	
	{	// 파일 열기
		const inputFile = document.createElement("input");
		inputFile.type = "file";
		inputFile.addEventListener("change", (e) => {
			if (inputFile.files.length) {
				inputUrl.value = input.src = URL.reset(inputFile.files[0]);
				inputFile.value = "";
			}
		});
		document.getElementById("btnOpenFile").addEventListener("click", async () => {
			inputFile.click();
		});
	}
	
	{	// 이미지 드래그해서 열기
		const cover = document.createElement("div");
		winPNG.querySelector("form").append(cover);
		cover.style.position = "absolute";
		cover.style.top = 0;
		cover.style.left = 0;
		cover.style.right = 0;
		cover.style.bottom = 0;
		cover.style.background = "rgba(127,127,127,0.1)";
		cover.style.display = "none";
		
		document.addEventListener("dragenter", (e) => {
			e.preventDefault();
			cover.style.display = "";
		});
		cover.addEventListener("dragleave", (e) => {
			e.preventDefault();
			cover.style.display = "none";
		});
		cover.addEventListener("dragover", (e) => {
			e.preventDefault();
		});
		cover.addEventListener("drop", async (e) => {
			e.preventDefault();
			cover.style.display = "none";
			if (e.dataTransfer) {
				if (e.dataTransfer.files && e.dataTransfer.files.length) {
					const file = e.dataTransfer.files[0];
					const ext = file.name.substring(file.name.length - 4);
					switch (ext.toLowerCase()) {
						case ".png":
						case ".bmp": {
							inputUrl.value = input.src = URL.reset(file);
							break;
						}
						case ".zip": {
							ivTarget.innerHTML = "";
							const comment = document.createElement("div");
							comment.id = "comment";
							comment.append("ZIP 파일입니다.");
							ivTarget.append(comment);
							inputUrl.value = URL.reset(file);
							unzip(file);
							break;
						}
						default: {
							ivTarget.innerHTML = "";
							const comment = document.createElement("div");
							comment.id = "comment";
							comment.append("단일 파일입니다.");
							ivTarget.append(comment);
							inputUrl.value = URL.reset(file);
							setOne(file);
						}
					}
				} else if (e.dataTransfer.items && e.dataTransfer.items.length) {
					let urlItem = null;
					[...e.dataTransfer.items].forEach((item) =>  {
						if (item.kind == "string" && item.type == "text/uri-list") {
							urlItem = item;
						}
					});
					if (urlItem) {
						let filename = e.dataTransfer.getData("text/uri-list").split('?')[0].split('/');
						filename = decodeURIComponent(filename[filename.length - 1]);
						urlItem.getAsString(async (url) => {
							try {
								const response = await fetch(url + (url.indexOf("?")<0 ? "?" : "") + "&_=" + Math.random(), {
										method: "GET"
									,	mdoe: "CORS"
								});
								const file = await response.blob();
								
								switch (file.type) {
									case "image/png":
									case "image/bmp": {
										inputUrl.value = input.src = URL.reset(file);
										break;
									}
									case "application/zip":
									case "application/octet-stream": {
										ivTarget.innerHTML = "";
										const comment = document.createElement("div");
										comment.id = "comment";
										comment.append("ZIP 파일입니다.");
										ivTarget.append(comment);
										inputUrl.value = URL.reset(file);
										if (!await unzip(file)) {
											comment.innerText = "단일 파일입니다.";
											setOne(file, filename);
										}
									}
								}
							} catch (e) {
								console.log(e);
								alert("CORS 문제 발생\n다른 방식으로 시도하세요.");
							}
						});
						
					} else {
						alert("이미지 드래그에 실패했습니다.\n지원되지 않는 환경이면 이미지 복사-붙여넣기로 시도하시기 바랍니다.");
					}
				}
			}
		});
		cover.addEventListener("click", (e) => {
			e.preventDefault();
			cover.style.display = "none";
		});
	}
	
	{	// 미리보기
		function addEL(els, en, selector, handler) {
			if (!Array.isArray(els)) {
				els = [els];
			}
			els.forEach((el) => {
				const wrapperHandler = handler
				?	(e) => {
						if (!e.target) return;
						const child = e.target.closest(selector);
						if (child) {
							handler.call(child, e);
						}
					}
				:	(e) => {
						selector.call(el, e);
					}
				;
				en.replace(" ", ",").split(",").forEach((e) => {
					el.addEventListener(e.trim(), wrapperHandler);
				});
			});
		}
		const previewContent  = document.getElementById("previewContent");
		const previewSelector = document.getElementById("previewSelector");
		addEL(viewFileList, "click", "span.preview", async (e) => {
			e.preventDefault();
			
			const a = e.srcElement.parentElement;
			let url = a.href;
			if (!url) { // JMK 파일 링크 제거한 경우
				url = a.getAttribute("data-href");
			}
			
			previewContent.innerHTML = "";
			switch (a.className) {
				case "text": {
					let withSelector = false;
					const radioJmk = previewSelector.querySelector("input[value=jmk]");
					const radioSmi = previewSelector.querySelector("input[value=smi]");
					const radioAss = previewSelector.querySelector("input[value=ass]");
					
					const filename = a.querySelector(".file").innerText;
					const extIndex = filename.lastIndexOf(".") + 1;
					const ext = filename.substring(extIndex).toLowerCase();
					
					switch (ext) {
						case "jmk": {
							const smi = a.getAttribute("data-smi");
							const ass = a.getAttribute("data-ass");
							
							if (withSelector = (smi || ass)) {
								radioJmk.parentElement.style.display = "";
								radioJmk.setAttribute("data-url", url);
								if (smi) {
									radioSmi.parentElement.style.display = "";
									radioSmi.setAttribute("data-url", a.getAttribute("data-smi"));
								} else {
									radioSmi.parentElement.style.display = "none";
								}
								if (ass) {
									radioAss.parentElement.style.display = "";
									radioAss.setAttribute("data-url", a.getAttribute("data-ass"));
								} else {
									radioAss.parentElement.style.display = "none";
								}
								radioJmk.click();
								withSelector = true;
							}
							break;
						}
						case "smi": {
							const ass = a.getAttribute("data-ass");
							if (withSelector = !!ass) {
								radioJmk.parentElement.style.display = "none";
								radioSmi.parentElement.style.display = "";
								radioSmi.setAttribute("data-url", a.getAttribute("data-url"));
								radioAss.parentElement.style.display = "";
								radioAss.setAttribute("data-url", a.getAttribute("data-ass"));
								radioSmi.click();
								withSelector = true;
							}
							break;
						}
						default: {
							withSelector = false;
						}
					}
					if (withSelector) {
						previewSelector.style.visibility = "visible";
					} else {
						previewContent.innerText = await URL.files[url].text();
						previewSelector.style.visibility = "hidden";
					}
					previewImg.style.display = "block";
					break;
				}
				case "image": {
					previewSelector.style.visibility = "hidden";
					const img = new Image();
					img.src = url;
					previewContent.append(img);
					previewImg.style.display = "block";
					break;
				}
			}
			previewContent.scrollTo(0,0);
		});
		addEL(previewSelector, "click", "input", async (e) => {
			const url = e.srcElement.getAttribute("data-url");
			previewContent.innerText = await URL.files[url].text();
		});
		document.getElementById("btnClosePreview").addEventListener("click", (e) => {
			previewImg.style.display = "";
		});
		document.addEventListener("keydown", (e) => {
			if (e.keyCode == 27) { // ESC
				if (previewImg.style.display == "block") {
					// 미리보기 닫기
					previewImg.style.display = "";
				} else {
					// 뷰어 닫기
					winPNG.classList.remove("on");
				}
			}
		});
		
		const form = winPNG.querySelector("form");
		cbJamaker = document.getElementById("cbJamaker");
		form.addEventListener("click", (e) => {
			if (e.target.closest("#cbJamaker")) {
				if (cbJamaker.checked) {
					[...viewFileList.children].forEach((a) => {
						if (!a.download.endsWith(".smi")) return;
						const cleared = a.getAttribute("data-cleared");
						if (cleared) {
							a.href = cleared;
						}
					});
				} else {
					[...viewFileList.children].forEach((a) => {
						if (!a.download.endsWith(".smi")) return;
						a.href = a.getAttribute("data-href");
					});
				}
				document.cookie = "cbJamaker=" + cbJamaker.checked;
				
			} else if (e.target.closest("input[name=priority]")) {
				document.cookie = "priority=" + form.priority.value;
			}
		});
		
		document.cookie.split(";").forEach((cookie) => {
			cookie = cookie.trim().split("=");
			if (cookie[0] == "cbJamaker") {
				cbJamaker.checked = (cookie[1] == "true");
			} else if (cookie[0] == "priority") {
				form.priority.value = cookie[1];
			}
		});
	}
	
	if (location.search.length) {
		const params = location.search.substring(1).split("&");
		for (let i = 0; i < params.length; i++) {
			if (params[i].startsWith("url=")) {
				inputUrl.value = decodeURIComponent(params[i].substring(4));
				parse();
				break;
			}
			if (params[i].startsWith("ref") && document.referrer) {
				inputUrl.value = document.referrer;
				parse();
				break;
			}
		}
	}
}

window.addEventListener("load", () => {
	setTimeout(() => {
		const link = document.createElement("link");
		link.rel = "stylesheet";
		link.href = new URL("./Viewer.css?260413", import.meta.url).href;
		document.head.append(link);
		
		// 사이드바 뷰 구성
		winPNG = document.createElement("div");
		winPNG.id = "winPNG";
		winPNG.innerHTML
			=	'<div id="toggleWinPNG"></div>'
			+	'<div id="externalFrame"><div>↑ 이미지 길게 터치 후 복사<br />↓ URL란에 길게 터치 후 붙여넣기</div></div>'
			+	'<form onsubmit="return parse();">'
			+		'<fieldset>'
			+			'<label for="inputUrl">URL:</label>'
			+			'<input type="text" id="inputUrl" autocomplete="off" />'
			+			'<button type="submit">열기</button>'
			+			'<button type="button" id="btnOpenFile">파일 열기</button>'
			+		'</fieldset>'
			+		'<div id="ivTarget">'
			+			'<div id="comment">이미지를 드래그하거나<br />Ctrl+V 하셔도 됩니다.</div>'
			+		'</div>'
			+		'<div id="introWinPNG"><a href="https://github.com/harnenim/WinPNG">[WinPNG 알아보기]</a></div>'
			+		'<div id="areaSettingZip" title="ZIP 파일에 포함할 파일 우선순위">ZIP 포함 우선순위<br />'
			+			'<label><input type="radio" name="priority" value="all" checked />모두</label>'
			+			'<label><input type="radio" name="priority" value="smi" />SMI</label>'
			+			'<label><input type="radio" name="priority" value="ass" />ASS</label>'
			+		'</div>'
			+		'<div id="areaSetting">'
			+			'<label title="MPC-HC, VLC 등 smi 파일의 주석이 노출되는 플레이어를 이용하는 경우에 필요합니다."><input type="checkbox" id="cbJamaker"> .smi 내 Jamaker용 주석 제거</label>'
			+			'<a href="javascript:downloadZip()">[ZIP으로 받기]</a>'
			+		'</div>'
			+		'<div id="areaFileList">'
			+			'<div id="viewFileList"></div>'
			+		'</div>'
			+		'<div id="previewImg">'
			+			'<div id="previewWindow">'
			+				'<div id="previewSelector">'
			+				'	<label><input type="radio" name="type" value="jmk" />JMK</label>'
			+				'	<label><input type="radio" name="type" value="smi" />SMI</label>'
			+				'	<label><input type="radio" name="type" value="ass" />ASS</label>'
			+				'</div>'
			+				'<div id="previewContent"></div>'
			+				'<button type="button" id="btnClosePreview">×</button>'
			+			'</div>'
			+		'</div>'
			+		'<div id="winPNGprogress"><div class="spinner"></div></div>'
			+	'</form>';
		document.body.append(winPNG);
		
		onload();
	}, 1);
});
