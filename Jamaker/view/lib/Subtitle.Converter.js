import "./SubtitleObject.js";

window.Combine = {
	css: 'font-family: 맑은 고딕;'
,	defaultSize: 18 // TODO: 설정에서 바뀌도록... 하려면 서브 프로그램에서도 설정을 불러와야 하는데...
};
{
	const STIME = 0;
	const STYPE = 1;
	const ETIME = 2;
	const ETYPE = 3;
	const TEXT  = 4;
	const ATTRS = 5;
	const LINES = 6;
	const WIDTH = 7;
	const SIZED = 8;
	
	const UPPER = 4;
	const LOWER = 5;
	
	const LOG = false;
	
	// 다중 결합에 대해 중간 싱크 처리를 위한 부분
	SyncType.combinedNormal = 4;
	SyncType.combinedFrame  = 5;
	SyncType.combinedInner  = 6;
	Smi.TypeParser[SyncType.combinedNormal] = "\t\t";
	Smi.TypeParser[SyncType.combinedFrame ] = " \t\t";
	Smi.TypeParser[SyncType.combinedInner ] = "\t\t\t";
	Smi._getSyncType = Smi.getSyncType;
	Smi.getSyncType = function(syncLine) {
		if (syncLine.endsWith("\t\t>")) {
			switch (syncLine[syncLine.length - 4]) {
				case '\t':
					return SyncType.combinedInner;
				case ' ':
					return SyncType.combinedFrame;
			}
			return SyncType.combinedNormal;
		}
		return Smi._getSyncType(syncLine);
	}
	
	function isClear(attr, br=null) {
		// 공백문자가 들어가도 무관한 속성
		return !attr.u
		    && !attr.s
		    && (br ? (br.fs == attr.fs) : !attr.fs)
		    && !attr.fn
		    && !attr.typing // 타이핑 같은 건 결합 전에 사라져야 함
		    && !attr.furigana;
	}
	function getAttrWidth(attrs, withFs=false) {
		const cAttrs = [];
		function append(attr) {
			const cAttr = new Attr(attr, attr.text.replaceAll("&nbsp;", " "), true);
			cAttr.fs = ((withFs && cAttr.fs) ? cAttr.fs : Combine.defaultSize);
			if (cAttr.fn && cAttr.fn != "맑은 고딕") {
				// 팟플레이어 폰트 크기 보정
				cAttr.fs = cAttr.fs * 586 / 456;
			}
			cAttr.furigana = null;
			cAttrs.push(cAttr);
		}
		attrs.forEach((attr) => {
			if (attr.attrs) {
				attr.attrs.forEach((subAttr) => {
					append(subAttr);
				});
			} else {
				append(attr);
			}
		});
		Combine.checker.innerHTML = Smi.fromAttr(cAttrs, Combine.defaultSize).replaceAll("\n", "<br>");
		const width = Combine.checker.clientWidth;
		if (LOG) console.log(width, attrs);
		return width;
	}
	function initChecker() {
		if (!Combine.checker) {
			Combine.checker = document.createElement("span");
			Combine.checker.classList.add("width-checker");
			document.body.append(Combine.checker);
			const _style = document.createElement("style");
			_style.innerHTML = "\n"
				+ "	.width-checker, .width-checker * {\n"
				+ "	white-space: pre;\n"
				+ "	font-size: 144px;\n"
				+ "	font-weight: bold;\n"
				+ "}";
			document.head.append(_style);
		}
		Combine.checker.setAttribute("style", Combine.css);
		Combine.checker.style.display = "inline-block";
	}
	
	function getSyncLine(sync, type) {
		let line = null;
		if (window.SmiEditor) {
			line = SmiEditor.makeSyncLine(sync, type + 1);
		} else {
			line = `<Sync Start=${sync}><P Class=KRCC${ Smi.TypeParser[type] }>`;
		}
		return line;
	}
	
	function parse(text) {
		const smis = new SmiFile(text).body;
		smis.push(new Smi(99999999, SyncType.normal, "&nbsp;"));
		
		const syncs = [];
		let last = null;
		smis.forEach((smi) => {
			if (last) { // 이전 것 남아있으면 종료싱크 부여
				last[ETIME] = smi.start;
				last[ETYPE] = smi.syncType;
				last = null;
			}
			if (smi.text.startsWith("<!--")) {
				// 결합 전 주석 제거
				let commentEnd = smi.text.indexOf("-->\n");
				if (commentEnd > 0) {
					smi.text = smi.text.substring(commentEnd + 4);
				}
			}
			if (smi.text.replaceAll("&nbsp;", "").trim()) {
				const lineCount = smi.text.split(/<br>/gi).length;
				
				const attrs = smi.toAttrs(false);
				const defaultWidth = getAttrWidth(attrs);
				const sizedWidth   = getAttrWidth(attrs, true);
				syncs.push(last = [smi.start, smi.syncType, 0, 0, smi.text, attrs, lineCount, defaultWidth, sizedWidth]);
			}
		});
		return syncs;
	}
	
	Combine.combine = (inputUpper, inputLower) => {
		const funcFrom = window.log ? log("combine start") : 0;
		
		// 결합 로직 돌아갈 때 문법 하이라이트가 있으면 성능 저하됨
		// ... 지금은 개선해서 큰 저하 없을지도?
		initChecker();
		const upperSyncs = parse(inputUpper);
		const lowerSyncs = parse(inputLower);
		
		const groups = [];
		{
			let group = null;
			let ui = 0;
			let li = 0;
			while  ((ui <= upperSyncs.length) && (li <= lowerSyncs.length)) {
				if ((ui == upperSyncs.length) && (li == lowerSyncs.length)) {
					break;
				}
				const us = (ui < upperSyncs.length) ? upperSyncs[ui] : [99999999, 99999999, null, 0];
				const ls = (li < lowerSyncs.length) ? lowerSyncs[li] : [99999999, 99999999, null, 0];
				if (us[STIME] < ls[STIME]) { // 위가 바뀜
					if (group
						&& (   (   (us[STYPE] == SyncType.inner) // 중간 싱크
						        || (us[STYPE] == SyncType.combinedInner)
						       )
						    || (group.lower.length && (group.lower[group.lower.length - 1][ETIME] > us[STIME]))
						   )
					) { // 그룹 유지
						group.upper.push(us);
						group.maxLines[0] = Math.max(group.maxLines[0], us[LINES]);
						group.maxWidth = Math.max(group.maxWidth, us[WIDTH]);
						group.maxSized = Math.max(group.maxSized, us[SIZED]);
						
					} else { // 아래가 없거나 끝남 -> 그룹 끊김
						groups.push(group = {
								upper: [us]
							,	lower: []
							,	maxLines: [us[LINES], 0]
							,	maxWidth: us[WIDTH]
							,	maxSized: us[SIZED]
						});
					}
					ui++;
					
				} else if (ls[STIME] < us[STIME]) { // 아래가 바뀜
					if (group
						&& (   (   (ls[STYPE] == SyncType.inner) // 중간 싱크
						        || (ls[STYPE] == SyncType.combinedInner)
						       )
						    || (group.upper.length && (group.upper[group.upper.length - 1][ETIME] > ls[STIME]))
						   )
					) { // 그룹 유지
						group.lower.push(ls);
						group.maxLines[1] = Math.max(group.maxLines[1], ls[LINES]);
						group.maxWidth = Math.max(group.maxWidth, ls[WIDTH]);
						group.maxSized = Math.max(group.maxSized, ls[SIZED]);
						
					} else { // 위가 없거나 끝남 -> 그룹 끊김
						groups.push(group = {
								upper: []
							,	lower: [ls]
							,	maxLines: [0, ls[LINES]]
							,	maxWidth: ls[WIDTH]
							,	maxSized: ls[SIZED]
						});
					}
					li++;
					
				} else { // 둘이 같이 바뀜
					if ((us[STYPE] == SyncType.inner) || (us[STYPE] == SyncType.combinedInner)
					 || (ls[STYPE] == SyncType.inner) || (ls[STYPE] == SyncType.combinedInner)) {
						// 하나라도 중간 싱크 - 그룹 유지
						group.upper.push(us);
						group.lower.push(ls);
						group.maxLines[0] = Math.max(group.maxLines[0], us[LINES]);
						group.maxLines[1] = Math.max(group.maxLines[1], ls[LINES]);
						group.maxWidth = Math.max(group.maxWidth, us[WIDTH]);
						group.maxWidth = Math.max(group.maxWidth, ls[WIDTH]);
						group.maxSized = Math.max(group.maxSized, us[SIZED]);
						group.maxSized = Math.max(group.maxSized, ls[SIZED]);
						
					} else {
						// 새 그룹
						groups.push(group = {
								upper: [us]
							,	lower: [ls]
							,	maxLines: [us[LINES], ls[LINES]]
							,	maxWidth: Math.max(us[WIDTH], ls[WIDTH])
							,	maxSized: Math.max(us[SIZED], ls[SIZED])
						});
					}
					ui++;
					li++;
				}
			}
		}
		groups.forEach((group) => {
			const withFontSize = group.maxSized < group.maxWidth;
			const groupMaxWidth = withFontSize ? group.maxSized : group.maxWidth;
			group.lines = [];
			let last = null;
			
			// 팟플레이어 왼쪽 정렬에서 좌우로 흔들리지 않도록 최대한 잡아줌
			if (group.upper.length && group.lower.length) {
				if (LOG) {
					console.log(group);
					console.log("group width: " + group.maxWidth);
					console.log("group sized width: " + group.maxSized);
				}
				
				[group.upper, group.lower].forEach((list) => {
					list.forEach((sync) => {
						// 줄 길이 채워주기
						const attrs = sync[ATTRS];
						if (LOG) {
							console.log(sync);
						}
						
						// 전체가 기본보다 작은 글씨로 감싸여 있을 경우
						// 과도한 줄바꿈 크기가 생기지 않도록 공백문자도 작게 넣어주기
						let maxFs = 0;
						for (let k = 0; k < attrs.length; k++) {
							const attr = attrs[k];
							if (attr.attrs) {
								const subAttrs = attr.attrs;
								for (let ki = 0; ki < subAttrs.length; ki++) {
									const subAttr = subAttrs[ki];
									if (subAttr.text) {
										if (!subAttr.text.replaceAll("　", "").replaceAll("​", "").trim()) {
											continue;
										}
										if (attr.fs) {
											maxFs = Math.max(maxFs, subAttr.fs);
										} else {
											maxFs = Combine.defaultSize;
											break;
										}
									}
								}
								
							} else if (attr.text) {
								if (!attr.text.replaceAll("　", "").replaceAll("​", "").trim()) {
									continue;
								}
								if (attr.fs) {
									maxFs = Math.max(maxFs, attr.fs);
								} else {
									maxFs = Combine.defaultSize;
									break;
								}
							}
						};
						
						let pad = "";
						if (sync[WIDTH] < group.maxWidth && sync[SIZED] < group.maxSized) {
							// 여백을 붙여서 제일 적절한 값 찾기
							if (withFontSize) {
								// 글씨 크기 적용했을 때 더 작아졌으면 이걸 기준으로 구함
								/*
								if (sync[WIDTH] > groupMaxWidth) {
									// 크기 조절 안 했을 때의 폭을 이미 넘어섰으면 작업 안 함
									if (LOG) console.log("width over");
									sync[TEXT] = Smi.fromAttr(attrs).replaceAll("\n", "<br>");
									continue;
								}
								*/
							}
							let isEmpty = true;
							let width = sync[SIZED];
							let padsAttrs = attrs;
							let lastPad;
							let lastAttrs;
							let lastWidth;
							let closeAttr = null;
							const br = new Attr(null, "\n");
							
							// 좌우 여백 붙이기 전 줄 단위로 분리 및 기존 Zero-Width-Space 삭제
							let wasClear = false;
							let trimedLine = { attrs: [], isEmpty: true };
							const trimedLines = [trimedLine];
							for (let k = 0; k < attrs.length; k++) {
								if (attrs[k].attrs) {
									attrs[k].attrs.forEach((subAttr) => {
										const attrText = subAttr.text.replaceAll("​", "");
										let attr = new Attr(subAttr, attrText, true);
										
										if (attrText.length == 0) {
											// 내용물 없는 속성 무시
											return;
										}
										
										if (attr.text) {
											trimedLine.attrs.push(attr);
										}
										wasClear = isClear(attr, br);
										if (trimedLine.isEmpty && attr.text.replaceAll("　", "").trim().length) {
											trimedLine.isEmpty = isEmpty = false;
										}
									});
									continue;
								}
								const attrText = attrs[k].text.replaceAll("​", "");
								let attr = new Attr(attrs[k], attrText, true);
								
								if (attrText.length == 0) {
									// 내용물 없는 속성 무시
									if (k < attrs.length - 1) {
										continue;
											
									} else {
										// 종료 태그
										if (wasClear) {
											// 앞쪽에 공백문자 넣을 수 있으면 따로 뺌
											closeAttr = attr;
											break;
										}
									}
								}
								
								const attrLines = attrText.split("\n");
								if (attrLines.length > 1) {
									// 속성 안에 줄바꿈이 있었으면 분리 후 진행
									attr.text = attrLines[0];
									if (attr.text) {
										trimedLine.attrs.push(attr);
										wasClear = isClear(attr, br);
									} else {
										// 첫 줄바꿈 전에 내용 없으면 건너뜀
										wasClear = false;
									}
									
									if (trimedLine.isEmpty && attr.text.replaceAll("　", "").trim().length) {
										trimedLine.isEmpty = isEmpty = false;
									}
									
									for (let l = 1; l < attrLines.length; l++) {
										attr = new Attr(attr, attrLines[l], true);
										const isEmptyAttr = (attr.text.replaceAll("​", "").trim().length == 0);
										if ((l == attrLines.length - 1) && isEmptyAttr) {
											// 마지막 줄바꿈 후에 내용 없으면 건너뜀
											trimedLines.push(trimedLine = { attrs: [], isEmpty: true });
										} else {
											attr.splitted = wasClear; // 재결합 대상
											trimedLines.push(trimedLine = { attrs: [attr], isEmpty: isEmptyAttr });
											if (!isEmptyAttr) {
												isEmpty = false;
											}
										}
									}
									
								} else {
									if (attr.text) {
										trimedLine.attrs.push(attr);
									}
									wasClear = isClear(attr, br);
									if (trimedLine.isEmpty && attr.text.replaceAll("　", "").trim().length) {
										trimedLine.isEmpty = isEmpty = false;
									}
								}
							}
							if (LOG) {
								console.log(attrs, trimedLines, isEmpty);
							}

							let checkThinSpace = false;
							let fullPad;
							let fullAttrs;
							let fullWidth;
							
							if (!isEmpty) {
								// 전체가 기본보다 작은 글씨로 감싸여 있을 경우
								// 과도한 줄바꿈 크기가 생기지 않도록 공백문자도 작게 넣어주기
								if (maxFs < Combine.defaultSize) {
									br.fs = maxFs;
								}
								do {
									if (checkThinSpace) {
										fullPad = pad;
										fullAttrs = padsAttrs;
										fullWidth = width;
										pad = lastPad + " "; // &ThinSpace;
									} else {
										lastPad = pad;
										lastAttrs = padsAttrs;
										lastWidth = width;
										pad = lastPad + " ";
									}
									padsAttrs = [];
									
									trimedLines.forEach((trimedLine, l) => {
										if (l > 0) {
											padsAttrs.push(br);
										}
										if (trimedLine.isEmpty) {
											// 빈 줄이면 그대로 추가
											padsAttrs.push(...trimedLine.attrs);
												
										} else {
											if (trimedLine.attrs.length == 0) {
												// 해당 줄에 속성이 없을 때..는 없는 게 맞음
													
											} else if (trimedLine.attrs.length == 1) {
												// 해당 줄에 속성이 하나일 때
												let attr = trimedLine.attrs[0];
												if (isClear(attr, br)) {
													// 속성에 공백문자 포함
													if (attr.splitted) {
														// 재결합 대상
														padsAttrs.pop(); // 미리 추가한 줄바꿈 제거
														const lastAttr = padsAttrs.pop();
														attr = new Attr(attr, lastAttr.text + "\n​" + pad + attr.text + pad + "​", true);
														padsAttrs.push(attr);
													} else {
														attr = new Attr(attr, "​" + pad + attr.text + pad + "​", true);
														padsAttrs.push(attr);
													}
												} else {
													// 속성 밖에 공백문자 추가
													padsAttrs.push(new Attr(br, "​" + pad));
													padsAttrs.push(attr);
													padsAttrs.push(new Attr(br, pad + "​"));
												}
													
											} else {
												// 해당 줄에 속성이 여러 개일 때
													
												// 처음 속성
												let attr = trimedLine.attrs[0];
												if (isClear(attr, br)) {
													// 속성에 공백문자 포함
													if (attr.splitted) {
														// 재결합 대상
														padsAttrs.pop(); // 미리 추가한 줄바꿈 제거
														const lastAttr = padsAttrs.pop();
														attr = new Attr(attr, lastAttr.text + "\n​" + pad + attr.text, true);
														padsAttrs.push(attr);
													} else {
														attr = new Attr(attr, "​" + pad + attr.text, true);
														padsAttrs.push(attr);
													}
												} else {
													// 속성 밖에 공백문자 추가
													padsAttrs.push(new Attr(br, "​" + pad));
													padsAttrs.push(attr);
												}
													
												// 중간 속성은 그대로 넣음
												for (let k = 1; k < trimedLine.attrs.length - 1; k++) {
													padsAttrs.push(trimedLine.attrs[k]);
												}
													
												// 마지막 속성
												attr = trimedLine.attrs[trimedLine.attrs.length - 1];
												if (isClear(attr, br)) {
													// 속성에 공백문자 포함
													padsAttrs.push(new Attr(attr, attr.text + pad + "​", true));
												} else {
													// 속성 밖에 공백문자 포함
													padsAttrs.push(attr);
													padsAttrs.push(new Attr(br, pad + "​"));
												}
											}
										}
									});
									if (closeAttr) {
										// 종료 태그 붙이기
										padsAttrs.push(closeAttr);
									} else {
										padsAttrs.push(new Attr()); // 종료태그 필수
									}
									width = getAttrWidth(padsAttrs, withFontSize);
									if (LOG) console.log(padsAttrs, width);
									
									if (width == groupMaxWidth || checkThinSpace) {
										break;
									}
									if (width > groupMaxWidth) {
										// ThinSpace 추가 검증
										//checkThinSpace = true;
										// 팟플레이어에서 실험 결과 일반 공백이랑 같은 폭을 차지함
										break;
									}
								} while (true);
							}
							
							if (LOG) console.log("width", groupMaxWidth, lastWidth, width, fullWidth);
							
							if ((width - groupMaxWidth) > (groupMaxWidth - lastWidth)) {
								// 마지막 공백문자 추가 안 한 게 더 폭이 비슷함
								pad = lastPad;
								padsAttrs = lastAttrs;
								width = lastWidth;
								if (LOG) console.log(padsAttrs, width);
								
							} else if (checkThinSpace && ((fullWidth - groupMaxWidth) < (groupMaxWidth - width))) {
								// 마지막에 ThinSpace 추가한 것보다 FullSpace 추가한 게 더 폭이 비슷함
								pad = fullPad;
								padsAttrs = fullAttrs;
								width = fullWidth;
								if (LOG) console.log(padsAttrs, width);
							}
							
							sync[TEXT] = Smi.fromAttr(padsAttrs).replaceAll("\n", "<br>");
							
						} else {
							sync[TEXT] = Smi.fromAttr(attrs).replaceAll("\n", "<br>");
						}
					});
				});
			}
			{	let ui = 0;
				let li = 0;
				while  ((ui <= group.upper.length) && (li <= group.lower.length)) {
					if ((ui == group.upper.length) && (li == group.lower.length)) {
						break;
					}
					const us = (ui < group.upper.length) ? group.upper[ui] : [99999999, 99999999, null, 0];
					const ls = (li < group.lower.length) ? group.lower[li] : [99999999, 99999999, null, 0];
						
					if (us[STIME] < ls[STIME]) { // 위가 바뀜
						if (!last) { // 첫 싱크
							group.lines.push(last = [us[STIME], us[STYPE], us[ETIME], us[ETYPE], us, null]);
							
						} else {
							// 아래는 유지하고 위는 바뀐 걸 추가
							if (last[STIME] == us[STIME]) {
								last[UPPER] = us;
							} else if (us[STIME] < last[ETIME]) {
								const curr = [us[STIME], us[STYPE], last[ETIME], last[ETYPE], us, last[LOWER]];
								last[ETIME] = us[STIME];
								last[ETYPE] = us[STYPE];
								group.lines.push(last = curr);
							}
							
							if (us[ETIME] < last[ETIME]) { // 위가 먼저 끝남
								const curr = [us[ETIME], us[ETYPE], last[ETIME], last[ETYPE], null, last[LOWER]];
								last[ETIME] = us[ETIME];
								last[ETYPE] = us[ETYPE];
								group.lines.push(last = curr);
							} else if (us[ETIME] > last[ETIME]) { // 아래가 먼저 끝남
								group.lines.push(last = [last[ETIME], last[ETYPE], us[ETIME], us[ETYPE], us, null]);
							} else {
								// 둘 다 끝남 -> 그룹 끝
							}
						}
						ui++;
						
					} else if (ls[STIME] < us[STIME]) { // 아래가 바뀜
						if (!last) { // 첫 싱크
							group.lines.push(last = [ls[STIME], ls[STYPE], ls[ETIME], ls[ETYPE], null, ls]);
							
						} else {
							// 위는 유지하고 아래는 바뀐 걸 추가
							if (last[STIME] == ls[STIME]) {
								last[LOWER] = ls;
							} else if (ls[STIME] < last[ETIME]) {
								const curr = [ls[STIME], ls[STYPE], last[ETIME], last[ETYPE], last[UPPER], ls];
								last[ETIME] = ls[STIME];
								last[ETYPE] = ls[STYPE];
								group.lines.push(last = curr);
							}
							
							if (ls[ETIME] < last[ETIME]) { // 아래가 먼저 끝남
								const curr = [ls[ETIME], ls[ETYPE], last[ETIME], last[ETYPE], last[TEXT], null];
								last[ETIME] = ls[ETIME];
								last[ETYPE] = ls[ETYPE];
								group.lines.push(last = curr);
							} else if (ls[ETIME] > last[ETIME]) { // 위가 먼저 끝남
								group.lines.push(last = [last[ETIME], last[ETYPE], ls[ETIME], ls[ETYPE], null, ls]);
							} else {
								// 둘 다 끝남 -> 그룹 끝
							}
						}
						li++;
						
					} else { // 둘이 같이 바뀜(그룹 첫 싱크에서만 가능)
						let ss = us;
						if (ls[ETIME] < us[ETIME]) {
							ss = ls;
							li++;
						} else {
							ui++;
						}
						group.lines.push(last = [us[STIME], us[STYPE], ss[ETIME], ss[ETYPE], us, ls]);
					}
				}
			}
		});
		Combine.checker.innerText = "";
		Combine.checker.style.display = "none";
		
		const lines = [];
		let lastSync = 0;
		groups.forEach((group, gi) => {
			const forEmpty = [[], []];
			for (let i = 0; i < 2; i++) {
				for (let j = 0; j < group.maxLines[i]; j++) {
					forEmpty[i].push("<b>　</b>");
				}
				forEmpty[i] = forEmpty[i].join("<br>");
			}
			
			group.lines.forEach((line, i) => {
				if (line[STIME] < 0) {
					// 건너뛰기
					return;
				}
				if (lastSync < line[STIME]) {
					if (gi > 0) { // 처음일 땐 제외
						lines.push("&nbsp;");
					}
					lines.push(getSyncLine(line[STIME], line[STYPE]));
				}
				if (group.upper.length == 0) {
					lines.push(line[LOWER] ? line[LOWER][TEXT] : "&nbsp;");
				} else if (group.lower.length == 0) {
					lines.push(line[UPPER] ? line[UPPER][TEXT] : "&nbsp;");
				} else {
					// 위쪽 채우지 않기
					if (line[UPPER]) {
						let text = line[UPPER][TEXT];
						// 중간 공백줄 채우기
						for (let j = (line[LOWER] ? line[LOWER][LINES] : 0); j < group.maxLines[1]; j++) {
							text += "<br><b>　</b>";
						}
						// 아랫줄 있으면 넣기
						if (line[LOWER]) {
							text += "<br>" + line[LOWER][TEXT];
						}
						lines.push(text);
							
					} else if (line[LOWER]) {
						// 아랫줄만 있을 때
						lines.push(line[LOWER][TEXT]);
							
					} else {
						// 그룹 내에서 둘 다 없을 수는 없음
					}
				}
				if (line[ETIME] < 99999999) {
					let syncLine = getSyncLine(lastSync = line[ETIME], line[ETYPE]);
					if (i < group.lines.length - 1 && !syncLine.endsWith("\t\t>")) {
						// 결합 시 임시 중간 싱크로 처리, 다중 결합 시 그룹화
						syncLine = syncLine.substring(0, syncLine.length - 1) + "\t\t>";
					}
					lines.push(syncLine);
				} else {
					lastSync = 0;
				}
			});
		});
		if (lastSync) {
			lines.push("&nbsp;");
		}
		if (window.log) log("combine end", funcFrom);
		
		return lines;
	}
}

SmiFile.textToHolds = (text) => {
	const funcFrom = window.log ? log("textToHolds start") : 0;
	
	const texts = text.replaceAll("\r\n", "\n").split("\n<!-- Hold=");
	let holds = [{ text: texts[0] }];
	for (let i = 1; i < texts.length; i++) {
		const hold = texts[i];
		const begin = hold.indexOf("\n");
		const end = hold.indexOf("-->");
		if (begin < 0 || end < 0) {
			holds[0].text += "\n<!-- Hold=" + hold;
			continue;
		}
		// Hold 내용물 뒤에 뭐가 더 붙어있을 경우
		if (end < hold.length - 3) {
			holds[0].text += hold.substring(end + 3);
		}
		let name = hold.substring(0, begin).trim();
		let pos = 1;
		const index = name.indexOf("|");
		if (index) {
			try {
				pos = Number(name.substring(0, index));
			} catch (e) {
				console.log(e);
			}
			name = name.substring(index + 1);
		}
		holds.push({
				pos: pos
			,	name: name
			,	text: hold.substring(begin, end).trim().replaceAll("<​", "<").replaceAll("​>", ">")
		});
	}
	
	// SMI 파일 역정규화
	const normalized = new SmiFile(holds[0].text).antiNormalize();
	normalized[0].pos = 0;
	normalized[0].name = "메인";
	
	// 내포 홀드를 뒤쪽에 추가해 선택기가 위로 올라오도록 함
	const exportedHoldsLength = holds.length;
	holds[0] = normalized[0];
	holds.push(...normalized.slice(1));
	
	{	// footer 정보 확인
		let footer = holds[0].footer;
		{	// 메인 홀드 ASS 변환용 스타일
			footer = footer.split("\n<!-- Style\n"); // <!-- Style 여러 번 있는 경우는 오류로, 상정하지 않음
			if (footer.length > 1) {
				const commentEnd = footer[1].indexOf("\n-->");
				if (commentEnd > 0) {
					holds[0].style = SmiFile.parseStyle(footer[1].substring(0, commentEnd).trim());
					footer = footer[0] + footer[1].substring(commentEnd + 4); // 뒤에 추가로 주석 남아있을 수 있음
				} else {
					footer = footer[0]; // 닫는 태그 없으면 군더더기로 간주해 제거
				}
			} else {
				footer = footer[0];
			}
		}
		{	// ASS 추가 스크립트
			footer = footer.split("\n<!-- ASS\n"); // <!-- ASS 여러 번 있는 경우는 오류로, 상정하지 않음
			if (footer.length > 1) {
				const commentEnd = footer[1].indexOf("\n-->");
				if (commentEnd > 0) {
					holds[0].ass = footer[1].substring(0, commentEnd).trim();
					footer = footer[0] + footer[1].substring(commentEnd + 4); // 뒤에 추가로 주석 남아있을 수 있음
				} else {
					footer = footer[0]; // 닫는 태그 없으면 군더더기로 간주해 제거
				}
			} else {
				footer = footer[0];
			}
		}
		{	// 프레임 시간
			footer = footer.split("\n<!-- FS\n"); // <!-- FS 여러 번 있는 경우는 오류로, 상정하지 않음
			if (footer.length > 1) {
				const commentEnd = footer[1].indexOf("\n-->");
				if (commentEnd > 0) {
					try {
						const lines = footer[1].substring(0, commentEnd).split("\n");
						if (lines.length > 0) {
							const fs = holds[0].fs = [0];
							const ftfs = new Uint16Array(Uint8Array.fromBase64(lines[0].trim()).buffer);
							let last = 0;
							ftfs.forEach((ftf) => {
								last += ftf;
								fs.push(last);
							});
						}
						if (lines.length > 1) {
							const fs = holds[0].kfs = [0];
							const ftfs = new Uint16Array(Uint8Array.fromBase64(lines[1].trim()).buffer);
							let last = 0;
							ftfs.forEach((ftf) => {
								last += ftf;
								fs.push(last);
							});
						}
						footer = footer[0] + footer[1].substring(commentEnd + 4); // 뒤에 추가로 주석 남아있을 수 있음
					} catch (e) {
						console.log(e);
						footer = footer[0];
					}
				} else {
					footer = footer[0]; // 닫는 태그 없으면 군더더기로 간주해 제거
				}
			} else {
				footer = footer[0];
			}
		}
		holds[0].footer = footer;
	}
	holds[0].text = holds[0].toText().trim();
	
	for (let i = exportedHoldsLength; i < holds.length; i++) {
		// 내포된 홀드는 종료싱크가 빠졌을 수 있음
		const hold = holds[i];
		if (hold.next && !hold.body[hold.body.length - 1].isEmpty()) {
			hold.body.push(new Smi(hold.next.start, hold.next.syncType, "&nbsp;"));
		}
		holds[i].text = hold.toText().trim();
		
		// 출력 선택 확인
		const names = holds[i].name.split("|");
		let output = 3;
		if (names.length > 1) {
			holds[i].name = names[0];
			if (names[1] == "X") {
				// 처음에 방향성을 잘못 잡음... 극히 일부 샘플에만 들어간 값
				output = 1;
			} else {
				output = Number(names[1]);
			}
		}
		// 홀드 스타일: antiNormalize 단계에서 가져옴
		let style = holds[i].style;
		if (!style) {
			holds[i].style = style = JSON.parse(JSON.stringify(Subtitle.DefaultStyle));
		}
		style.output = output;
	}
	for (let i = 1; i < exportedHoldsLength; i++) {
		const hold = new SmiFile(holds[i].text).antiNormalize()[0];
		let text = (holds[i].text = hold.toText().trim());
		let lines = text.split("\n");
		
		// 출력 선택 확인
		const names = holds[i].name.split("|");
		let output = 3;
		if (names.length > 1) {
			holds[i].name = names[0];
			if (names[1] == "X") {
				// 처음에 방향성을 잘못 잡음... 극히 일부 샘플에만 들어간 값
				output = 1;
			} else {
				output = Number(names[1]);
			}
		}
		// 홀드 스타일: header 확인
		{	let style = null;
			if ((lines[0] == "<!-- Style" || lines[0] == "<!-- Preset") && lines[2] == "-->") {
				style = holds[i].style = SmiFile.parseStyle(lines[1].trim());
				text = (lines = lines.slice(3)).join("\n");
			} else {
				style = JSON.parse(JSON.stringify(Subtitle.DefaultStyle));
			}
			style.output = output;
			holds[i].style = style;
		}
		holds[i].text = text;
	}
	
	if (window.log) log("textToHolds end", funcFrom);
	
	return holds;
}

SmiFile.holdsToParts = (origHolds, withNormalize=true, withCombine=true, withComment=1) => {
	// withComment: 원래 true/false였는데
	// 1: true / 0: false / -1: Jamaker 전용 싱크 표시 같은 것까지 제거하도록 변경
	
	const funcFrom = window.log ? log("holdsToParts start") : 0;
	
	const result = [];
	let logs = [];
	let originBody = [];
	
	const main = new SmiFile(origHolds[0].text);
	if (withComment > 0) {
		// 메인 홀드 스타일 저장
		const style = SmiFile.toSaveStyle(origHolds[0].style);
		if (style) {
			main.footer += `\n<!-- Style\n${style}\n-->`;
		}
	}
	
	withCombine = withCombine && origHolds.length > 1;
	
	{	// 시작 시간 순으로 저장
		const holdsWithoutMain = origHolds.slice(1);
		holdsWithoutMain.sort((a, b) => {
			return a.start - b.start;
		});
		
		// 홀드 결합 대상 확인
		const imports = [];
		holdsWithoutMain.forEach((hold, hi) => {
			const holdText = hold.text;
			let text = holdText;
			hold.exportName = hold.name;
			if (hold.style) {
				const style = SmiFile.toSaveStyle(hold.style);
				if (style) {
					text = `<!-- Style\n${style}\n-->\n` + text;
				}
				if (hold.style.output != 3) {
					// 출력 선택
					hold.exportName += "|" + hold.style.output;
				}
			}
			result[hold.resultIndex = (hi + 1)] = `<!-- Hold=${hold.pos}|${hold.exportName}\n${text.replaceAll("<", "<​").replaceAll(">", "​>")}\n-->`;
			hold.imported = false;
			hold.afterMain = false;
			
			// 홀드 위치가 1 또는 -1인 경우에만 내포 홀드 여부 확인
			if ((hold.pos > 1) || (hold.pos < -1)) {
				return;
			}
			
			if (hold.style) {
				// SMI 출력 없으면 내포 홀드 처리하지 않음
				if (!(hold.style.output & 0x01)) {
					// 홀드 결합 대상에선 제외되도록 완료 처리
					hold.imported = true;
					return;
				}
				// 스타일 적용 필요하면 내포 홀드 처리하지 않음
				const style = hold.saveStyle = SmiFile.toSaveStyle(hold.style);
				if (style) {
					// ASS용 스타일은 내포 홀드 처리함. SMI용 스타일이 적용된 경우만 제외
					if (hold.style.PrimaryColour != "#FFFFFF") return;
					if (hold.style.Italic   ) return;
					if (hold.style.Underline) return;
					if (hold.style.StrikeOut) return;
				}
			}
			
			// 내용물 없으면 내포 홀드 아님
			const holdBody = new SmiFile(holdText).body;
			if (holdBody.length == 0) {
				return;
			}
			if (!hold.end) {
				hold.end = holdBody[holdBody.length - 1].start;
			}
			
			if (main.body.length) {
				// 메인 홀드보다 뒤에 있는지 확인
				const i = main.body.length;
				const lastLine = main.body[i - 1];
				if ((lastLine.start <= hold.start) && lastLine.isEmpty()) {
					hold.afterMain = true;
					if (withCombine) {
						let hasImport = false;
						for (let j = 0; j < imports.length; j++) {
							const imported = imports[j];
							if (imported[0] == i) {
								// 기존 내포 홀드와 겹치면 내포 홀드 불가능
								if (hold.start < imported[1].end) {
									hasImport = true;
									break;
								}
							}
						}
						if (!hasImport) {
							// 내포 홀드는 결합 대상에서 제외
							imports.push([i, hold, holdBody]);
							result[hold.resultIndex] = "";
							hold.imported = true;
						}
					}
				}
				if (withCombine && !hold.imported) {
					for (let i = 0; i < main.body.length; i++) {
						const line = main.body[i];
						if (hold.start < line.start) {
							if (hold.end <= line.start) {
								if ((i == 0) || main.body[i - 1].isEmpty()) {
									let hasImport = false;
									for (let j = 0; j < imports.length; j++) {
										const imported = imports[j];
										if (imported[0] == i) {
											// 기존 내포 홀드와 겹치면 내포 홀드 불가능
											if (hold.start < imported[1].end) {
												hasImport = true;
												break;
											}
										}
									}
									if (!hasImport) {
										// 내포 홀드는 결합 대상에서 제외
										imports.push([i, hold, holdBody]);
										result[hold.resultIndex] = "";
										hold.imported = true;
									}
								}
							}
							break;
						}
					}
				}
				
			} else {
				// 메인 홀드가 비어있음
				hold.afterMain = true;
				
				if (withCombine) {
					// 메인 홀드 없으면 서로 겹치지만 않으면 내포 홀드 처리
					// TODO: 둘 중 하나가 다른 하나에 이중 내포 되는 경우 처리 필요?
					
					let hasImport = false;
					for (let j = 0; j < imports.length; j++) {
						if (hold.start < imports[j][1].end) {
							hasImport = true;
							break;
						}
					}
					if (!hasImport) {
						// 내포 홀드는 결합 대상에서 제외
						imports.push([0, hold, holdBody]);
						result[hold.resultIndex] = "";
						hold.imported = true;
					}
				}
			}
		});
		// 내포 홀드 처리
		let lastStart = 999999999;
		for (let i = imports.length - 1; i >= 0; i--) {
			const index = imports[i][0];
			const hold = imports[i][1];
			const importBody = imports[i][2];
			const removePrev = (index > 0 && main.body[index - 1].start == hold.start);
			let holdEnd = hold.end;
			if (index < main.body.length) {
				if (hold.end == main.body[index].start) {
					importBody.pop();
				}
			}
			if (hold.afterMain && (holdEnd < lastStart)) {
				// 메인 홀드보다 뒤쪽에 독립된 경우, 종료싱크를 잡아주기 위해 +1
				holdEnd++;
			}
			lastStart = hold.start;
			
			if (withComment > 0) {
				importBody[0].text = `<!-- End=${holdEnd}\nHold=${hold.pos}|${hold.exportName}`
					+ (hold.saveStyle ? "\n" + hold.saveStyle : "")
					+ "\n-->\n" + importBody[0].text;
			}
			main.body = main.body.slice(0, (removePrev ? index - 1 : index)).concat(importBody).concat(main.body.slice(index));
		}
	}
	
	// 정규화 등 작업
	if (withNormalize) {
		const normalized = main.normalize((withComment > 0) && !withCombine);
		originBody = normalized.origin;
		logs = normalized.logs;
	} else {
		if (origHolds.length > 1) {
			originBody = main.body.slice(0, main.body.length);
		}
	}
	
	if (withCombine) {
		// 메인에 가까운 걸 먼저 작업해야 함
		// 단, 아래쪽부터 쌓아야 함
		const holds = origHolds.slice(0);
		holds.sort((a, b) => {
			let aPos = a.viewPos;
			let bPos = b.viewPos;
			if (aPos < 0) {
				if (bPos > 0) {
					return -1;
				}
			} else {
				if (bPos < 0) {
					return 1;
				}
			}
			if (aPos < 0) aPos = -aPos;
			if (bPos < 0) bPos = -bPos;
			if (aPos < bPos) return -1;
			if (aPos > bPos) return 1;
			return 0;
		});
		
		const holdSmis = [];
		for (let hi = 1; hi < holds.length; hi++) {
			const hold = holds[hi];
			if (hold.imported) {
				continue;
			}
			const holdText = hold.text;
			let text = holdText;
			if (hold.style) {
				const style = (typeof hold.style == "string") ? hold.style : SmiFile.toSaveStyle(hold.style);
				if (style) {
					text = `<!-- Style\n${style}\n-->\n` + text;
				}
			}
			const smi = holdSmis[hi] = new SmiFile(text);
			if (withNormalize) {
				smi.normalize(false);
			}
			{	// 실질 내용물 없으면 공백으로 변환 후 처리
				smi.body.forEach((item) => {
					let syncText = item.text;
					if (syncText.startsWith("<!--")) {
						const endComment = syncText.indexOf("-->");
						if (endComment > 0) {
							syncText = syncText.substring(endComment + 3).trim();
						}
					}
					if (syncText.replaceAll("&nbsp;", "").trim().length == 0) {
						item.text = "&nbsp;";
					}
				});
			}
			
			if (smi.body.length == 0) {
				continue;
			}
			
			// 메인에서 홀드와 겹치는 영역 찾기
			let mainBegin = 0;
			let mainEnd = 0;
			{
				const start = smi.body[0].start;
				for (let i = 0; i <= main.body.length; i++) {
					if (i == main.body.length) {
						mainBegin = i;
						break;
					}
					if (main.body[i].start >= start) {
						break;
					}
					mainBegin = i;
				}
				if (mainBegin == main.body.length) {
					// 홀드 전체가 메인보다 뒤에 있음
					// 위쪽 내포 홀드에서 처리돼서 여기 올 일 없어졌을 듯 <- 스타일 넣으면서 생겼을 듯?
					main.body = main.body.concat(smi.body);
					continue;
				}
				if (main.body[mainBegin].isEmpty()) {
					mainBegin++;
				} else {
					// 중간 싱크는 함께 결합돼야 함
					while (mainBegin >= 0) {
						if ((main.body[mainBegin].syncType == SyncType.inner)
							|| (main.body[mainBegin].syncType == SyncType.combinedNormal)
							|| (main.body[mainBegin].syncType == SyncType.combinedFrame)
							|| (main.body[mainBegin].syncType == SyncType.combinedInner)
						) {
							mainBegin--;
						} else {
							break;
						}
					}
				}
				
				mainEnd = mainBegin;
				const last = smi.body[smi.body.length - 1];
				let isEnded = last.isEmpty();
				if (!isEnded) {
					// 미완성 자막인 경우 과도한 결합 발생
					// 마지막 대사가 5줄 넘어가면 미완성본으로 간주하고 종료 싱크로 처리, 결합 대상에서 제외
					isEnded = (last.text.split("\n").length > 5);
				}
				if (isEnded) {
					// 마지막 싱크가 종료 싱크일 경우 결합 범위 찾기
					const end = last.start;
					for (; mainEnd < main.body.length; mainEnd++) {
						if (main.body[mainEnd].start > end) {
							break;
						}
					}
				} else {
					// 대사가 남은 채 끝날 경우 끝까지 결합
					mainEnd = main.body.length;
				}
				if (mainEnd == 0) {
					// 홀드 전체가 메인보다 앞에 있음
					// 위쪽 내포 홀드에서 처리돼서 여기 올 일 없어졌을 듯 <- 스타일 넣으면서 생겼을 듯?
					main.body = smi.body.concat(main.body);
					continue;
				}
			}
			
			// 홀드 결합
			const sliced = new SmiFile();
			sliced.body = main.body.slice(mainBegin, mainEnd);
			
			const slicedText = sliced.toText().trim();
			const combineText = smi.toText().trim();
			const combined = new SmiFile(((hold.pos < 0) ? Combine.combine(slicedText, combineText) : Combine.combine(combineText, slicedText)).join("\n"));
			// 원칙상 normalized.result를 다뤄야 맞을 것 같지만...
			main.body = main.body.slice(0, mainBegin).concat(combined.body).concat(main.body.slice(mainEnd));
		}
		
		{	// 최종본에서 그룹 단위 윗줄 채워주기
			let begin = 0;
			let maxLine = 0;
			main.body.forEach((smi, i) => {
				const line = smi.text.split("<br>").length;
				if (smi.syncType == SyncType.combinedNormal
				 || smi.syncType == SyncType.combinedFrame
				 || smi.syncType == SyncType.combinedInner) {
					maxLine = Math.max(maxLine, line);
					
				} else {
					const end = i;
					if (end - begin > 1) {
						for (let j = begin; j < end; j++) {
							const item = main.body[j];
							const add = maxLine - item.text.split("<br>").length;
							for (let k = 0; k < add; k++) {
								item.text = "<b>　</b><br>" + item.text;
							}
						}
					}
					
					maxLine = line;
					begin = i;
				}
			});
		}
		
		// 임시 중간 싱크 정상화
		main.body.forEach((smi) => {
			if (smi.syncType == SyncType.combinedNormal) {
				smi.syncType = SyncType.normal;
			} else if (smi.syncType == SyncType.combinedFrame) {
				smi.syncType = SyncType.frame;
			} else if (smi.syncType == SyncType.combinedInner) {
				smi.syncType = SyncType.inner;
			}
		});
		
		// 프레임 단위로 볼 때 싱크 뭉친 부분 확인
		if (Subtitle.video.fs.length) {
			const fs = Subtitle.video.fs;
			let next = null;
			for (let i = main.body.length - 1; i > 0; i--) {
				const smi = main.body[i];
				if (next) {
					if (next.syncType == SyncType.frame) {
						// 1프레임 미만 중간싱크로 인해 화면싱크가 밀리는지 확인
						const startIndex = Subtitle.findSyncIndex(smi .start, fs);
						const endIndex   = Subtitle.findSyncIndex(next.start, fs);
						if (endIndex != null && startIndex == endIndex) {
							// 현재 대사 건너뛰기
							main.body.splice(i, 1);
							continue;
						}
					}
				}
				next = smi;
			}
		}
		
		if (withComment > 0) {
			// 홀드 결합 있을 경우 주석처리 재계산
			logs = [];
			let oi = 0;
			let ni = 0;
			
			while ((oi < originBody.length) && (ni < main.body.length)) {
				if (originBody[oi].start == main.body[ni].start) {
					if (originBody[oi].text == main.body[ni].text) {
						oi++;
						ni++;
						continue;
					} else if (originBody[oi].isEmpty() && main.body[ni].isEmpty()) {
						// 파트 구분 등을 위해 의도적으로 &nbsp; 2개 넣거나 한 경우가 있음
						// 결합 결과가 아닌 원본대로 넣어, 불필요한 주석 생성 방지
						main.body[ni].text = originBody[oi].text;
						oi++;
						ni++;
						continue;
					} else if (originBody[oi].text.startsWith("<!--")) {
						const commentEnd = originBody[oi].text.indexOf("-->\n");
						if (commentEnd > 4 && originBody[oi].text.substring(commentEnd + 4) == main.body[ni].text) {
							// 주석 때문에 다르게 나온 거면 원본대로 넣어, 불필요한 주석 생성 방지 
							main.body[ni].text = originBody[oi].text;
							oi++;
							ni++;
							continue;
						}
					}
				}
				
				// 변환결과가 원본과 동일하지 않은 범위 찾기
				const newLog = {
						from: [oi, originBody.length]
					,	to  : [ni, main.body.length]
					,	start: main.body[ni].start
					,	end: 999999999
				};
				while ((oi < originBody.length) && (ni < main.body.length)) {
					if (originBody[oi].start < main.body[ni].start) {
						oi++;
						continue;
					}
					if (originBody[oi].start > main.body[ni].start) {
						ni++;
						continue;
					}
					if (originBody[oi].text != main.body[ni].text) {
						oi++;
						ni++;
						continue;
					}
					// 싱크-내용 모두 동일한 곳 찾음
					newLog.from[1] = oi;
					newLog.to  [1] = ni;
					newLog.end = main.body[ni].start;
					break;
				}
				if ((newLog.start == newLog.end) // 의도대로라면 여길 들어오는 건 연속 공백싱크인 경우뿐임
				 && (newLog.from[0] > 0)
				 && (newLog.to  [0] > 0)
				) {
					const oPrev = originBody[newLog.from[0] - 1];
					const nPrev = main.body [newLog.to  [0] - 1];
					if (oPrev.start == nPrev.start && oPrev.text == nPrev.text) {
						newLog.start = oPrev.start;
						newLog.from[0] = newLog.from[0] - 1;
						newLog.to  [0] = newLog.to  [0] - 1;
					}
				}
				logs.push(newLog);
			}
			// 메인 홀드에 없는 내용만 남음
			if (ni < main.body.length) {
				logs.push({
						from: [oi, oi]
					,	to  : [ni, main.body.length]
					,	start: main.body[ni].start
					,	end  : main.body[main.body.length - 1].start + 1
				});
			}
			
			const origin = new SmiFile();
			logs.forEach((log) => {
				if (!log.end) {
					if (log.from[1] < originBody.length - 1) {
						log.end = originBody[log.from[1]].start;
					} else {
						log.end = 999999999;
					}
				}
				origin.body = originBody.slice(log.from[0], log.from[1]);
				let comment = origin.toText().trim();
				
				main.body[log.to[0]].text = `<!-- End=${log.end}\n${ comment.replaceAll("<", "<​").replaceAll(">", "​>") }\n-->\n` + main.body[log.to[0]].text;
			});
		}
	}
	
	if (withComment > 0) {
		for (let i = 1; i < result.length; i++) {
			if (result[i].length == 0) {
				result.splice(i--, 1);
			}
		}
		
	} else {
		if (withComment < 0) {
			// export 속성 제거
			main.header = main.header.replace(/<sami( [^>]*)*>/gi, "<SAMI>");
			main.body.forEach((smi) => {
				// 싱크 타입까지 제거
				smi.syncType = SyncType.normal;
				// ASS 변환용 주석도 제거
				if (smi.text.startsWith("<!-- ASS")) {
					const commentEnd = smi.text.indexOf("-->");
					if (commentEnd > 0) {
						smi.text = smi.text.substring(commentEnd + 3).trim();
					}
				}
			});
		}
		main.body.forEach((smi) => {
			smi.text = smi.text.replaceAll("\n", "");
		});
		result.length = 1;
	}
	result[0] = main;
	
	if (window.log) log("holdsToParts end", funcFrom);
	
	return result;
}
SmiFile.holdsToTexts = (holds, withNormalize=true, withCombine=true, withComment=1) => {
	const parts = SmiFile.holdsToParts(holds, withNormalize, withCombine, withComment);
	parts[0] = parts[0].toText();
	return parts;
}
SmiFile.holdsToText = (holds, withNormalize=true, withCombine=true, withComment=1, additional="", withFs=false, withKfs=false) => {
	if (Subtitle.video.fs.length && withFs) {
		// 프레임 싱크 함께 저장
		let fs = [];
		holds.forEach((hold) => {
			let last = { index: 0, text: "" };
			const smis = new SmiFile(hold.text).body;
			smis.forEach((smi, j) => {
				const index = Subtitle.findSyncIndex(smi.start);
				if ((last.text.indexOf("fade"  ) > 0)
				 || (last.text.indexOf("typing") > 0)
				 || (last.text.indexOf("shake" ) > 0)
				) { // 정확한 문법 체크를 안 해서 과도하게 들어갈 싱크는 얼마 되지 않을 것
					for (let i = last.index + 1; i <= index; i++) {
						fs.push(Subtitle.video.fs[i]);
					}
				} else {
					if (index > 0) fs.push(Subtitle.video.fs[index - 1]);
					fs.push(Subtitle.video.fs[index]);
				}
				last = {
						index: index
					,	text: smi.text.toLowerCase()
				};
				
				// ASS 싱크 확장 고려
				let assTexts = [];
				if (smi.text.startsWith("<!-- ASS")) {
					const commentEnd = smi.text.indexOf("-->");
					if (commentEnd > 0) {
						const assCmTexts = smi.text.substring(8, commentEnd).split("\n");
						smi.text = smi.text.substring(commentEnd + 3).trim();
						for (let j = 0; j < assCmTexts.length; j++) {
							const assLine = assCmTexts[j].trim();
							if (assLine == "") {
								continue;
							}
							if (assLine == "END" || assLine == "X") {
								break;
							}
							assTexts.push(assLine);
						}
					}
				}
				if (assTexts.length == 0) return;
	
				// ASS 싱크 확장 확인
				assTexts.forEach((assText) => {
					let ass = assText.split(",");
					if (ass.length < 5) return;
					if (!isFinite(ass[0])) return;
					
					if (ass[1] == "") { // span 형식
						if (ass[3].endsWith(")")) {
							let ass2 = ass[2].split("(");
							let ass3 = ass[3].split(")");
							if (ass2.length == 2
							 && isFinite(ass2[0])
							 && isFinite(ass2[1])
							 && isFinite(ass3[0])
							) {
								// [Layer, -, span(add, add), Style, Text]
								const span = Number(ass2[0]);
								{
									const index = Subtitle.findSyncIndex(smi.start + Number(ass2[1]));
									if (index > 0) fs.push(Subtitle.video.fs[index - 1]);
									fs.push(Subtitle.video.fs[index]);
								}
								if (smis.length > (j + span)) {
									const index = Subtitle.findSyncIndex(smis[j + span].start + Number(ass3[0]));
									if (index > 0) fs.push(Subtitle.video.fs[index - 1]);
									fs.push(Subtitle.video.fs[index]);
								}
							}
						}
					} else if (isFinite(ass[1])) { // add 형식
						// [Layer, addStart, addEnd, Style, Text]
						// [Layer, addStart, -, Style, Text]
						const addStart = Number(ass[1]);
						{
							const index = Subtitle.findSyncIndex(smi.start + addStart);
							if (index > 0) fs.push(Subtitle.video.fs[index - 1]);
							fs.push(Subtitle.video.fs[index]);
						}
						const addEnd = isFinite(ass[2]) ? Number(ass[2]) : addStart;
						{
							let end = smi.start;
							if (!ass[2].startsWith("+")) {
								if (smis.length <= j + 1) return;
								end = smis[j + 1].start;
							}
							const index = Subtitle.findSyncIndex(end + addEnd);
							if (index > 0) fs.push(Subtitle.video.fs[index - 1]);
							fs.push(Subtitle.video.fs[index]);
						}
					}
				});
			});
		});
		fs.push(Subtitle.video.fs[Subtitle.video.fs.length - 1]); // 마지막 싱크 필요할 수 있음
		(fs = [...new Set(fs)]).sort((a, b) => { return a - b; }); // 중복 제외 후 정렬
		
		// 프레임값 대신 프레임 간격을 16비트 정수로 저장
		const ftfs = [];
		{	let last = 0;
			fs.forEach((f) => {
				let ftf = f - last;
				if (ftf == 0) return;
				while (ftf > 65535) { // 싱크 간격이 65535ms를 넘어갈 경우 - 대사가 적으면 있을 수 있음
					ftfs.push(65535);
					ftf -= 65535;
				}
				ftfs.push(ftf);
				last = f;
			});
		}
		additional += "\n<!-- FS\n" + new Uint8Array(new Uint16Array(ftfs).buffer).toBase64();
		if (withKfs) {
			const ktfs = [];
			{	let last = 0;
				Subtitle.video.kfs.forEach((f) => {
					let ftf = f - last;
					if (ftf == 0) return;
					while (ftf > 65535) { // 키프레임 간격이 65535ms를 넘어갈 경우 - 어지간해선 존재하지 않는데, Philosophy 얘넨 있을 수 있음[..]
						ftfs.push(65535);
						ftf -= 65535;
					}
					ktfs.push(ftf);
					last = f;
				});
			}
			additional += new Uint8Array(new Uint16Array(ktfs).buffer).toBase64();
		}
		additional += "\n-->";
	}
	return SmiFile.holdsToTexts(holds, withNormalize, withCombine, withComment).join("\n") + additional;
}
SmiFile.partsToText = (parts) => {
	parts[0] = parts[0].toText();
	return parts.join("\n");
}

SmiFile.holdsToAss = function(holds, appendParts=[], appendStyles=[], appendEvents=[], playResX=1920, playResY=1080, orderByEndSync=false) {
	const funcSince = window.log ? log("holdsToAss start") : 0;
	
	const assFile = new AssFile(null, playResX, playResY);
	
	// 스타일/이벤트는 뺐다가
	const assStyles = assFile.getStyles();
	const assEvents = assFile.getEvents();
	assFile.parts.length = 1;
	// ASS 홀드 추가 스크립트 먼저 추가하고
	assFile.parts.push(...appendParts);
	// 뒤쪽에 다시 추가
	assFile.parts.push(assStyles);
	assFile.parts.push(assEvents);
	
	// ASS 홀드에서 추가한 내용 먼저 추가
	assEvents.body.push(...appendEvents);
	
	const syncs = [];
	const styles = {};
	
	holds.forEach((hold, h) => {
		const name = (h == 0) ? "Default" : hold.name;
		const style = hold.style ?? Subtitle.DefaultStyle;
		
		if ((style.output & 0b10) == 0) {
			// ASS 변환 대상 제외
			syncs.push(hold.syncs = []);
			hold.smiFile = null;
			return;
		}
		
		if (styles[name]) {
			// 이미 추가한 스타일은 건너뜀
		} else {
			assFile.addStyle(name, style, hold);
			styles[name] = style;
		}
		
		if (h == 0) {
			// 메인 홀드에서 <title> 확인
			const h0 = hold.smiFile.header.search(/<title>/gi);
			if (h0 > 0) {
				const h1 = hold.smiFile.header.search(/<\/title>/gi);
				if (h1 > 0) {
					const title = hold.smiFile.header.substring(h0 + 7, h1);
					assFile.getInfo().body.push({ key: "Title", value: title });
				}
			}
		}
		
		const smis = hold.smiFile.body;
		
		const assComments = []; // ASS 주석에서 복원한 목록
		const toAssEnds = {};
		smis.forEach((smi, i) => {
			{	// 앞에서 나온 ASS 형태에 종료싱크 채워주기
				const toAssEnd = toAssEnds[i];
				if (toAssEnd) {
					toAssEnd.forEach((item) => {
						if (item.end) return;
						item.end = smi.start + item.addEnd;
					});
				}
			}
			
			let assTexts = [];
			// 'END\n-->' 대신 'END -->', 'X -->' 등의 표현도 사용 가능
			if (smi.text.startsWith("<!-- ASS")) {
				const commentEnd = smi.text.indexOf("-->");
				if (commentEnd > 0) {
					const assCmTexts = smi.text.substring(8, commentEnd).split("\n");
					smi.text = smi.text.substring(commentEnd + 3).trim();
					for (let j = 0; j < assCmTexts.length; j++) {
						const assLine = assCmTexts[j].trim();
						if (assLine == "") {
							continue;
						}
						if (assLine == "END" || assLine == "X") {
							// ASS 변환 대상 제외
							smi.skip = true;
							// 이후 내용은 있더라도 무시
							break;
						}
						assTexts.push(assLine);
					}
				}
			}
			
			// ASS 주석에 [TEXT] 있을 경우 넣을 내용물 ([SMI]는 후처리 필요해서 빼둠)
			let smiText = htmlToText(smi.text.replaceAll(/<br>/gi, "\\N"));
			while (smiText.indexOf("\\N　\\N") >= 0) { smiText = smiText.replaceAll("\\N　\\N", "\\N"); }
			while (smiText.indexOf("\\N\\N"  ) >= 0) { smiText = smiText.replaceAll("\\N\\N"  , "\\N"); }
			while (smiText.startsWith("\\N")) { smiText = smiText.substring(2); }
			while (smiText.endsWith("\\N　")) { smiText = smiText.substring(0, smiText.length - 3); }
			while (smiText.endsWith("\\N"  )) { smiText = smiText.substring(0, smiText.length - 2); }
			
			// ASS 주석에서 복원
			assTexts.forEach((assText) => {
				const ass = assText.replaceAll("[TEXT]", smiText)
				                 .replaceAll("\n", "") // 비태그 줄바꿈은 무시해야 함
				                 .split(",");
				const item = {
						smi: smi
					,	ass: assText
					,	layer: 0
					,	start: smi.start
					,	end: 0
					,	addEnd: 0
					,	style: name
					,	text: ""
				};
				let span = 1;
				
				if (isFinite(ass[0])) {
					item.layer = ass[0];
					let type = null;
					
					if (ass.length >= 5) {
						if (ass[1] == "") { // span 형식
							type = "span";
							if (ass[2] == "") {
								// [Layer, -, -, Style, Text]
								if (ass[3]) item.style = ass[3];
								item.text = ass.slice(4).join(",");
								
							} else if (isFinite(ass[2])) {
								// [Layer, -, span, Style, Text]
								span = Number(ass[2]);
								if (ass[3]) item.style = ass[3];
								item.text = ass.slice(4).join(",");
								
							} else if (ass[3].endsWith(")")) {
								let ass2 = ass[2].split("(");
								let ass3 = ass[3].split(")");
								if (ass2.length == 2
								 && isFinite(ass2[0])
								 && isFinite(ass2[1])
								 && isFinite(ass3[0])
								) {
									// [Layer, -, span(add, add), Style, Text]
									span = Number(ass2[0]);
									item.start += Number(ass2[1]);
									item.addEnd = Number(ass3[0]);
									if (ass[4]) item.style = ass[4];
									item.text = ass.slice(5).join(",");
								}
							}
						} else if (isFinite(ass[1])) { // add 형식
							type = "add";
							const addStart = Number(ass[1]);
							item.start += addStart;
							if (isFinite(ass[2])) {
								// [Layer, addStart, addEnd, Style, Text]
								item.addEnd = Number(ass[2]);
								if (ass[2].startsWith("+")) { // +로 시작할 경우 시작 싱크를 기준으로
									item.end = item.start + item.addEnd;
								}
							} else {
								// [Layer, addStart, -, Style, Text]
								item.addEnd = addStart;
							}
							if (ass[3]) item.style = ass[3];
							item.text = ass.slice(4).join(",");
						}
					}
					if (!type) {
						// 싱크 변형 없이 스타일만 지정한 형식
						// [Layer, Style, Text]
						item.style = ass[1];
						item.text = ass.slice(2).join(",");
					}
				} else {
					// 텍스트만 입력: 홀드 스타일을 따라감
					item.text = ass.join(",");
				}
				
				// span만큼 경과한 싱크에 기반해서 종료싱크 부여해야 함
				let toAssEnd = toAssEnds[i + span];
				if (toAssEnd == null) {
					toAssEnd = toAssEnds[i + span] = [];
				}
				toAssEnd.push(item);
				assComments.push(item);
			});
		});
		if (assComments.length && assComments[assComments.length - 1].end == 0) {
			// 마지막에 종료싱크 없을 때
			assComments[assComments.length - 1].end = 999999999;
		}
		
		// 주석 기반 스크립트
		assComments.forEach((item) => {
			const event = new AssEvent(item.start, item.end, item.style, item.text, item.layer);
			event.owner = item.smi;
			event.comment = item.ass;
			assEvents.body.push(event);
			
			// [SMI]는 후처리 결과를 반영해야 함
			if (event.comment.indexOf("[SMI]") >= 0) {
				if (!event.owner.preAss) {
					event.owner.preAss = [];
				}
				event.owner.preAss.push(event);
			}
		});
		
		// SMI 기반 스크립트
		syncs.push(hold.syncs = hold.smiFile.toSyncs());
	});
	{	// 홀드 결합 pos 자동 조정
		const an2Holds = [];
		holds.forEach((hold) => {
			if (hold.style
			 && !(   hold.style.Alignment == 2
			      || hold.style.Alignment == 5
			     )
			) return; // ASS에서 정중앙 혹은 중앙 하단이 아니면 제외
			an2Holds.push(hold);
		});
		// 아래인 것부터 정렬
		an2Holds.sort((a, b) => {
			return a.pos - b.pos;
		});
		
		if (an2Holds.length > 1) {
			const usedLines = []; // 각 싱크에 사용된 줄 수
			an2Holds.forEach((hold) => {
				hold.syncs.forEach((sync) => {
					let useBottom = true; // an2Holds에 애초에 걸러진 것만 있음
					for (let j = 0; j < sync.text.length; j++) {
						const ass = sync.text[j].ass;
						if (ass && (ass.indexOf("\\an") > 0)) {
							const an = ass[ass.indexOf("\\an") + 3];
							if (an % 3 != 2) {
								// 개별적으로 좌우 구석에 밀었을 경우 제외
								useBottom = false;
								break;
							}
						}
					}
					if (!useBottom) {
						// \pos를 지정했으면 좌우 구석이 아니므로 중앙 높이를 차지할 수 있음
						for (let j = 0; j < sync.text.length; j++) {
							const ass = sync.text[j].ass;
							if (ass && (ass.indexOf("\\pos") > 0)) {
								useBottom = true;
								break;
							}
						}
					}
					if (!useBottom) {
						return;
					}
					
					let used = 0;
					
					// 이미 확인된 줄과 비교
					let j = 0
					for (; j < usedLines.length; j++) {
						if (sync.start == usedLines[j].start) {
							used = usedLines[j].used;
							break;
						} else if (sync.start < usedLines[j].start) {
							used = (j > 0) ? usedLines[j - 1].used : 0;
							break;
						}
					}
					
					const nextLines = [];
					let k = j;
					for (; k < usedLines.length; k++) {
						if (sync.end == usedLines[k].start) {
							break;
						} else if (sync.end < usedLines[k].start) {
							nextLines.push({ start: sync.end, used: ((k > 0) ? usedLines[k - 1].used : 0) });
							break;
						}
						used = Math.max(used, usedLines[k].used);
					}
					if (k == usedLines.length) {
						// 뒤쪽이 없었으면 끝내기 잡아줌
						nextLines.push({ start: sync.end, used: 0 });
					}
					
					sync.bottom = used;
					if (!sync.origin.skip) { // SMI 무시한 경우엔 더하지 않음
						used += sync.getTextOnly().split("\n").length;
					}
					
					nextLines.push(...usedLines.slice(k));
					usedLines.length = j;
					if (j > 0 && usedLines[j - 1].used == used) {
						// 앞쪽이랑 같으면 건너뜀
					} else {
						usedLines.push({ start: sync.start, used: used });
					}
					usedLines.push(...nextLines);
				});
			});
		}
	}
	for (let h = 1; h < holds.length; h++) {
		// SMI에서 용도가 다른 <b> 태그 속성 없애고 진행
		syncs[h].forEach((sync) => {
			sync.text.forEach((attr) => {
				if (attr.b) attr.b = false;
			});
		});
		// 홀드 내용물 추가
		assFile.addFromSyncs(syncs[h], holds[h].name);
	}
	// 메인 홀드를 마지막에 추가
	assFile.addFromSync(syncs[0], "Default");
	
	// 홀드에 없는 스타일 추가
	assStyles.body.push(...appendStyles);
	
	const eventsBody = assFile.getEvents().body;
	{	// ASS 자막은 SMI와 싱크 타이밍이 미묘하게 달라서 보정 필요
		if (Subtitle.video.fs.length) {
			for (let i = appendEvents.length; i < eventsBody.length; i++) {
				const item = eventsBody[i];
				item.Start = AssEvent.toAssTime((item.start = Subtitle.findSync(item.start)) - 15);
				item.End   = AssEvent.toAssTime((item.end   = Subtitle.findSync(item.end  )) - 15);
				if (item.start == 0) item.start = 1;
			}
		} else {
			// 프레임 값 없으면 fps 기반으로 계산
			const FL = Subtitle.video.FL;
			for (let i = appendEvents.length; i < eventsBody.length; i++) {
				const item = eventsBody[i];
				item.Start = Math.max(1, ((Math.round(item.start / FL) - 0.5) * FL) - 15);
				item.End   = Math.max(1, ((Math.round(item.end   / FL) - 0.5) * FL) - 15);
			}
		}
		
		eventsBody.forEach((item) => {
			item.clearEnds();
		});
	}
	
	// 원래의 스크립트 순서를 기준으로, 시간이 겹치는 걸 기준으로 레이어 재부여
	let forLayers = [];
	eventsBody.forEach((item) => {
		let forLayer = forLayers[item.Layer];
		if (!forLayer) {
			forLayers[item.Layer] = forLayer = [];
		}
		forLayer.push(item);
	});
	let checkeds = [];
	forLayers.forEach((forLayer) => {
		if (!forLayer) return;
		forLayer.forEach((item) => {
			let maxLayer = -1;
			checkeds.forEach((checked) => {
				if (item.start < checked.end && checked.start < item.end) {
					maxLayer = Math.max(maxLayer, checked.Layer);
				}
			});
			item.Layer = maxLayer + 1;
			checkeds.push(item);
		});
	});
	
	if (window.log) log("holdsToAss end", funcSince);
	
	if (orderByEndSync) {
		// 레이어 보장된 상태에서 종료싱크까지 정렬
		eventsBody.sort((a, b) => {
			let cmp = a.start - b.start;
			if (cmp == 0) {
				cmp = a.end - b.end;
			}
			return cmp;
		});
	} else {
		// 저장 시엔 레이어 순으로 정렬
		eventsBody.sort((a, b) => {
			let cmp = a.start - b.start;
			if (cmp == 0) {
				cmp = a.Layer - b.Layer;
			}
			return cmp;
		});
	}
	return assFile;
}