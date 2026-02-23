// java.awt.image.BufferedImage
window.BufferedImage = function(w, h) {
	if (typeof w == "object") {
		this.image = w;
		w = this.image.naturalWidth;
		h = this.image.naturalHeight;
		this.canvas = document.createElement("canvas");
		this.canvas.width  = this.width  = w;
		this.canvas.height = this.height = h;
		
		this.context = this.canvas.getContext("2d");
		// 1:1로 리사이즈 없이 그려도
		// 안드로이드 크롬/엣지 등 일부 브라우저에선
		// Smoothing이 동작해서 이웃 픽셀 색이 섞이는 경우가 발생
		this.context.imageSmoothingEnabled = false;
		this.context.drawImage(this.image, 0, 0, w, h);
		
	} else {
		this.canvas = document.createElement("canvas");
		this.canvas.width  = this.width  = w;
		this.canvas.height = this.height = h;
		this.context = this.canvas.getContext("2d");
	}
	this.data = this.context.getImageData(0, 0, this.canvas.width, this.canvas.height);
}
BufferedImage.prototype.getWidth  = function() { return this.width ; };
BufferedImage.prototype.getHeight = function() { return this.height; };
BufferedImage.prototype.getRGB = function(x, y) {
	const index = 4 * ((this.width * y) + x);
	return (this.data.data[index  ] << 16)
	     | (this.data.data[index+1] <<  8)
	     | (this.data.data[index+2]      );
}
BufferedImage.prototype.setRGB = function(x, y, rgb) {
	const index = 4 * ((this.width * y) + x);
	this.data.data[index  ] = (rgb >> 16) & 0xFF;
	this.data.data[index+1] = (rgb >>  8) & 0xFF;
	this.data.data[index+2] = (rgb      ) & 0xFF;
	this.data.data[index+3] = 0xFF;
}
BufferedImage.prototype.getRGBs = function(offsetX, offsetY, width, height) {
	const result = [];
	for (let x = 0; x < width; x++) {
		for (let y = 0; y < height; y++) {
			result[(width * y) + x] = this.getRGB(offsetX + x, offsetY + y);
		}
	}
	return result;
}
BufferedImage.prototype.update = function() {
	this.context.putImageData(this.data, 0, 0);
	return this;
}

// moe.ohli.pngb.Container
function pad0(str, len) {
	return pad(str, len, '0');
}
function toHex(value, len) {
	return pad(value.toString(16), len, '0');
}
function pad(str, len, pad) {
	while (str.length < len) {
		str = pad + str;
	}
	return str;
}

function pathLengthFromRGB(rgb) {
	if ((rgb&0xFFFF00) == 0) {
		return rgb & 0xFF; // 레거시 지원
	}
	return ((rgb&0x070000) >> 11) | ((rgb&0x000300) >> 5) | (rgb&0x000007);
}

function bytesToRGBs(bytes) {
	const result = new Array(Math.floor((bytes.length + 2) / 3));
	for (let i = 0; i < result.length; i++) {
		result[i] = 0;
	}
	for (let i = 0; i < bytes.length; i++) {
		result[Math.floor(i / 3)] |= ((bytes[i] & 0xFF) << ((2 - (i % 3)) * 8)) & 0xFFFFFF;
	}
	return result;
}
function getShift(key) {
	return fibonacci(key.length);
}
function getXors(key) {
	const bytes = new TextEncoder().encode(key);
	for (let i = 0; i < bytes.length; i++) {
		bytes[i] =                      // 75361420
				( ((bytes[i] << (7-0)) & 0b10000000)
				| ((bytes[i] << (5-1)) & 0b01000000)
				| ((bytes[i] << (3-2)) & 0b00100000)
				| ((bytes[i] << (6-3)) & 0b00010000)
				| ((bytes[i] << (1-4)) & 0b00001000)
				| ((bytes[i] << (4-5)) & 0b00000100)
				| ((bytes[i] << (2-6)) & 0b00000010)
				| ((bytes[i] << (0-7)) & 0b00000001)
				);
	}
	return bytesToRGBs(bytes);
}
function fibonacci(no) {
	return subFibonacci(0, 1, no - 1);
}
function subFibonacci(before, current, left) {
	if (left < 0) {
		return before;
	} else if (left == 0) {
		return current;
	} else {
		return subFibonacci(current, before + current, left - 1);
	}
}

window.Container = function(rgbs, shift=0, xors=[]) {
	console.debug("pathLength  : " + toHex(rgbs[ shift    % rgbs.length], 8));
	console.debug("binaryLength: " + toHex(rgbs[(shift+1) % rgbs.length], 8));
	// xor 연산 수행
	if (xors.length > 0) {
		console.debug("xors0       : " + toHex(xors[ shift    % xors.length], 8));
		console.debug("xors1       : " + toHex(xors[(shift+1) % xors.length], 8));
		for (let i = 0; i < rgbs.length; i++) {
			rgbs[i] ^= xors[i % xors.length];
		}
	}
	const pathLength = pathLengthFromRGB(rgbs[ shift    % rgbs.length]);
	const binaryLength = 0xFFFFFF &      rgbs[(shift+1) % rgbs.length];
	console.debug("pathLength  : " + pathLength);
	console.debug("binaryLength: " + binaryLength);
	
	this.setDataFromRGBs(rgbs, shift, pathLength, binaryLength);
	
};
Container.prototype.setDataFromRGBs = function(rgbs, shift, pathLength, binaryLength) {
	if (binaryLength > 20971520) {
		throw new Exception("이미지 해석 오류");
	}
	
	let offset = shift + 2;
	
	// RGB
	console.debug("RGB");
	let bytes = new Uint8Array(pathLength);
	for (let i = 0; i < pathLength; i++) {
		bytes[i] = ((rgbs[(offset + Math.floor(i/3)) % rgbs.length] >> (8 * (2-(i%3)))) & 0xFF);
	}
	console.debug("pathBytes: " + bytes.length);
	this.path = new TextDecoder("UTF-8").decode(bytes);
	offset += Math.floor((pathLength + 2) / 3);
	
	this.binary = new Uint8Array(binaryLength);
	for (let i = 0; i < binaryLength; i++) {
		this.binary[i] = ((rgbs[(offset + Math.floor(i/3)) % rgbs.length] >> (8 * (2-(i%3)))) & 0xFF);
	}
}
/**
 * 비트맵 이미지를 컨테이너 목록으로 변환
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return 컨테이너 목록
 * @throws Exception
 */
Container.fromBitmap = async function(bmp, shift=0, xors=[]) {
	console.info("\nContainer.fromBitmap");
	let containers = [];
	
	let offsetY = 0;
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	shift = shift % width;
	console.info("input size: " + width + " x " + height);
	
	while (offsetY < height) {
		console.debug("shift: " + shift);
		console.debug("xors.length: " + xors.length);
		let pathLength   = 0xFFFFFF & bmp.getRGB( shift    % width, offsetY);
		let binaryLength = 0xFFFFFF & bmp.getRGB((shift+1) % width, offsetY);
		console.debug("pathLength  : " + toHex(pathLength  , 8));
		console.debug("binaryLength: " + toHex(binaryLength, 8));
		if (xors.length > 0) {
			console.debug("xors0       : " + toHex(xors[ shift    % xors.length], 8));
			console.debug("xors1       : " + toHex(xors[(shift+1) % xors.length], 8));
			pathLength   = (pathLength   ^ xors[ shift    % xors.length]) & 0xFFFFFF;
			binaryLength = (binaryLength ^ xors[(shift+1) % xors.length]) & 0xFFFFFF;
		}
		pathLength = pathLengthFromRGB(pathLength);
		const pixelCount = 2 + Math.ceil(pathLength / 3) + Math.ceil(binaryLength / 3);
		const contHeight = Math.floor((pixelCount + width - 1) / width);
		console.debug("");
		console.debug("pathLength  : " + pathLength);
		console.debug("binaryLength: " + binaryLength);
		console.debug("pixelCount: " + pixelCount);
		console.debug("contHeight: " + contHeight);
		if (pathLength > 255) { // pathLengthFromRGB 적용하면서 이렇게 나올 일이 없음...
			console.warn("잘못된 경로 길이: " + pathLength);
			break;
		}
		if (offsetY + contHeight > height) {
			console.warn("잘못된 데이터 크기: " + offsetY + contHeight > height);
			break;
		}
		
		const rgbs = bmp.getRGBs(0, offsetY, width, contHeight);
		const cont = new Container(rgbs, shift, xors);
		console.debug("path: " + cont.path);
		
		if (pathLength == 0) {
			// 경로가 없음: 이중 변환 or 정크 영역
			try {
				// 이중 변환으로 가정하고 해석 시도
				const subImg = new Image();
				subImg.src = URL.createObjectURL(new Blob([cont.binary], { type: "image/png" }), true);
				await subImg.decode();
				
				containers = containers.concat(await Container.fromBitmap(new BufferedImage(subImg)));
				URL.revokeObjectURL(subImg.src);
				
			} catch (e) {
				console.info("JUNK DATA");
			}
		} else {
			// 잘못된 경로: 해석 실패 - 이것만으론 불충분할 수도...
			if (cont.path.indexOf("*") >= 0
			 || cont.path.indexOf("?") >= 0
			 || cont.path.indexOf('"') >= 0
			 || cont.path.indexOf("<") >= 0
			 || cont.path.indexOf(">") >= 0
			 || cont.path.indexOf("|") >= 0
			 || cont.path.indexOf("\\") >= 0 // 모두 /으로 치환돼있어야 함
			 || cont.path.indexOf("�") >= 0
					) {
				//throw new Exception("잘못된 경로");
				console.warn("잘못된 경로: " + cont.path);
				break;
			}
			// 경로가 있음: 일반 파일
			containers.push(cont);
		}
		
		offsetY += contHeight;
	}
	
	return containers;
}

// moe.ohli.pngb.Container.WithTarget
const TYPE_114v1 = 1;
const TYPE_149   = 2;
const TYPE_238   = 3;
const TYPE_429   = 4;
const TYPE_114v2 = 5;
const TYPE_114v3 = 6;
const TYPE_124   = 7;
const TYPE_114 = TYPE_114v3;

const SAMPLING_UNIT = 7; // 패리티 검증 체크섬 샘플링 단위
const MIN_SAMPLE_COUNT = 10; // 패리티 검증 체크섬 샘플 최소 갯수
function getChecksumPoints(w, h) {
	const points = [];
	const xCount = Math.floor((w + SAMPLING_UNIT - 1) / SAMPLING_UNIT);
	const yCount = Math.floor((h + SAMPLING_UNIT - 1) / SAMPLING_UNIT);
	const count = xCount * yCount;
	
	if (count < MIN_SAMPLE_COUNT) {
		for (let i = 0; i < MIN_SAMPLE_COUNT; i++) {
			points.push([
					Math.floor((Math.random() * w) / 2)
				,	Math.floor((Math.random() * h) / 2)
			]);
		}
	} else {
		for (let y = 0; y < yCount; y++) {
			for (let x = 0; x < xCount; x++) {
				points.push([
						x * SAMPLING_UNIT + Math.floor(Math.random() * Math.min(SAMPLING_UNIT, w - (x * SAMPLING_UNIT)))
					,	y * SAMPLING_UNIT + Math.floor(Math.random() * Math.min(SAMPLING_UNIT, h - (y * SAMPLING_UNIT)))
				]);
			}
		}
	}
	return points;
}
const CAN_PROTOTYPE = 1;
const CAN_114v1 = 1 << TYPE_114v1;
const CAN_149   = 1 << TYPE_149;
const CAN_238   = 1 << TYPE_238;
const CAN_429   = 1 << TYPE_429;
const CAN_114v2 = 1 << TYPE_114v2;
const CAN_114v3 = 1 << TYPE_114v3;
const CAN_124   = 1 << TYPE_124;

window.WithTarget = function(targetImage, containers, type) {
	this.targetImage = targetImage;
	this.containers = containers;
	this.type = type;
};

/**
 * 패리티 검증으로 해석 가능한 알고리즘 확인
 * @param bmp
 * @return 가능한 알고리즘
 */
WithTarget.possibility = function(bmp) {
	let result = 0;
	if (WithTarget.canPrototype(bmp)) { result |= CAN_PROTOTYPE; }
	if (WithTarget.can114v3    (bmp)) { result |= CAN_114v3;     }
	if (WithTarget.can114v2    (bmp)) { result |= CAN_114v2;     }
	if (WithTarget.can114      (bmp)) { result |= CAN_114v1;     }
	if (WithTarget.can149      (bmp)) { result |= CAN_149;       }
	if (WithTarget.can238      (bmp)) { result |= CAN_238;       }
	if (WithTarget.can429      (bmp)) { result |= CAN_429;       }
	if (WithTarget.can124      (bmp)) { result |= CAN_124;       }
	return result;
}

// b = (a+d)/₂
function getB(a, d) {
	//     (                a           +  d                        ) / 2
	return ( ((            (a&0xFF0000) + (d&0xFF0000)) & 0x1FE0000)
	       | ((            (a&0x00FF00) + (d&0x00FF00)) & 0x001FE00)
	       | ((            (a&0x0000FF) + (d&0x0000FF)) & 0x00001FE)) >> 1;
}
// c = (1+a-d)/₂
function getC(a, d) {
	//     (     1       +  a           -  d                        ) / 2
	return ( ((0x1000000 + (a&0xFF0000) - (d&0xFF0000)) & 0x1FE0000)
	       | ((0x0010000 + (a&0x00FF00) - (d&0x00FF00)) & 0x001FE00)
	       | ((0x0000100 + (a&0x0000FF) - (d&0x0000FF)) & 0x00001FE)) >> 1;
}
// {½ + b - c} = {½ + (a+d)/₂ - (1+a-d)/₂} = {(1+a+d-1-a+d)/₂} = d
function getD(b, c) {
	//      1/2      +  b           -  c
	return (0x800000 + (b&0xFF0000) - (c&0xFF0000))
	     | (0x008000 + (b&0x00FF00) - (c&0x00FF00))
	     | (0x000080 + (b&0x0000FF) - (c&0x0000FF));
	
}
// {½ + a - b - c} = {½ + 2a/₂ - (a+d)/₂ - (1+a-d)/₂} = {(1+2a-a-d-1-a+d)/₂} = 0
function isValid(a, b, c, d) {
	//              1/2      +  a           -  b           -  c
	const checksum = (0x800000 + (a&0xFF0000) - (b&0xFF0000) - (c&0xFF0000))
	             + (0x008000 + (a&0x00FF00) - (b&0x00FF00) - (c&0x00FF00))
	             + (0x000080 + (a&0x0000FF) - (b&0x0000FF) - (c&0x0000FF));

	console.debug("{ " + a
	           + " / " + b
	           + " / " + c
	           + " / " + d
	           + " } -> checksum: " + checksum);
	console.debug("{ " + toHex(a, 6)
	           + " / " + toHex(b, 6)
	           + " / " + toHex(c, 6)
	           + " / " + toHex(d, 6)
	           + " } -> checksum: " + toHex(checksum, 6));
	
	const compare = (((a&0x010101)+(d&0x010101))&0x010101); // 정수연산 한계로 a+d가 홀수일 땐 1이 나옴
	
	if (checksum == compare) {
		return true;
	} else {
		console.debug("is not " + toHex(compare, 6));
		return false;
	}
}

function getBp2x(ax, dx, bx) {
	return (ax < 0x80 == dx < 0x80) ? 0x80 : ((0x280 + ax - 2*dx) / 8 * 2 + 1);
}
function getCp2x(ax, dx, cx) {
	return (ax < 0x80 != dx < 0x80) ? 0x80 : ((0x80 + ax + 2*dx) / 8 * 2);
}
function getAp2x(ax, bx, cx, dx) {
	return 0x180 - getBp2x(ax, dx, bx) - getCp2x(ax, dx, cx);
}
function getBv2(a, d, b) {
	return ( ((b&0xFF0000) + (getBp2x((a >> 16) & 0xFF, (d >> 16) & 0xFF, (b >> 16) & 0xFF) << 16) - 0x800000)
	       | ((b&0x00FF00) + (getBp2x((a >>  8) & 0xFF, (d >>  8) & 0xFF, (b >>  8) & 0xFF) <<  8) - 0x008000)
	       | ((b&0x0000FF) + (getBp2x((a >>  0) & 0xFF, (d >>  0) & 0xFF, (b >>  0) & 0xFF) <<  0) - 0x000080) );
}
function getCv2(a, d, c) {
	return ( ((c&0xFF0000) + (getCp2x((a >> 16) & 0xFF, (d >> 16) & 0xFF, (c >> 16) & 0xFF) << 16) - 0x800000)
	       | ((c&0x00FF00) + (getCp2x((a >>  8) & 0xFF, (d >>  8) & 0xFF, (c >>  8) & 0xFF) <<  8) - 0x008000)
	       | ((c&0x0000FF) + (getCp2x((a >>  0) & 0xFF, (d >>  0) & 0xFF, (c >>  0) & 0xFF) <<  0) - 0x000080) );
}
function getAv2(a, b, c, d) {
	return ( ((a&0xFF0000) + (getAp2x((a >> 16) & 0xFF, (b >> 16) & 0xFF, (c >> 16) & 0xFF, (d >> 16) & 0xFF) << 16) - 0x800000)
	       | ((a&0x00FF00) + (getAp2x((a >>  8) & 0xFF, (b >>  8) & 0xFF, (c >>  8) & 0xFF, (d >>  8) & 0xFF) <<  8) - 0x008000)
	       | ((a&0x0000FF) + (getAp2x((a >>  0) & 0xFF, (b >>  0) & 0xFF, (c >>  0) & 0xFF, (d >>  0) & 0xFF) <<  0) - 0x000080) );
}
function getAp(a, an) {
	return 0x808080 + an - a;
}
function b2toB(bn, ap) {
	return 0x808080 + bn
			- (((ap&0x010000) == 0x000000) ? 0x800000 : (0x1000000 - (ap&0xFF0000)))
			- (((ap&0x000100) == 0x000000) ? 0x008000 : (0x0010000 - (ap&0x00FF00)))
			- (((ap&0x000001) == 0x000000) ? 0x000080 : (0x0000100 - (ap&0x0000FF)));
}
function c2toC(cn, ap) {
	return 0x808080 + cn
			- (((ap&0x010000) == 0x010000) ? 0x800000 : (0x1000000 - (ap&0xFF0000)))
			- (((ap&0x000100) == 0x000100) ? 0x008000 : (0x0010000 - (ap&0x00FF00)))
			- (((ap&0x000001) == 0x000001) ? 0x000080 : (0x0000100 - (ap&0x0000FF)));
}

// for 1:1:4 v3
function getBp3(a, d) {
	return ( ((((a&0x800000) ^ (d&0x800000)) == 0x000000) ? (0xC00000 - (((0x0800000 + (a&0xFF0000) + 2*(d&0xFF0000)) >> 3) & 0xFE0000)) : (((0x2800000 + (a&0xFF0000) - 2*(d&0xFF0000)) >> 2) & 0xFC0000) | 0x010000)
	       | ((((a&0x008000) ^ (d&0x008000)) == 0x000000) ? (0x00C000 - (((0x0008000 + (a&0x00FF00) + 2*(d&0x00FF00)) >> 3) & 0x00FE00)) : (((0x0028000 + (a&0x00FF00) - 2*(d&0x00FF00)) >> 2) & 0x00FC00) | 0x000100)
	       | ((((a&0x000080) ^ (d&0x000080)) == 0x000000) ? (0x0000C0 - (((0x0000080 + (a&0x0000FF) + 2*(d&0x0000FF)) >> 3) & 0x0000FE)) : (((0x0000280 + (a&0x0000FF) - 2*(d&0x0000FF)) >> 2) & 0x0000FC) | 0x000001) );
}
function getCp3(a, d) {
	return ( ((((a&0x800000) ^ (d&0x800000)) == 0x800000) ? (0xC00000 - (((0x2800000 + (a&0xFF0000) - 2*(d&0xFF0000)) >> 3) & 0xFE0000)) : (((0x0800000 + (a&0xFF0000) + 2*(d&0xFF0000)) >> 2) & 0xFC0000))
	       | ((((a&0x008000) ^ (d&0x008000)) == 0x008000) ? (0x00C000 - (((0x0028000 + (a&0x00FF00) - 2*(d&0x00FF00)) >> 3) & 0x00FE00)) : (((0x0008000 + (a&0x00FF00) + 2*(d&0x00FF00)) >> 2) & 0x00FC00))
	       | ((((a&0x000080) ^ (d&0x000080)) == 0x000080) ? (0x0000C0 - (((0x0000280 + (a&0x0000FF) - 2*(d&0x0000FF)) >> 3) & 0x0000FE)) : (((0x0000080 + (a&0x0000FF) + 2*(d&0x0000FF)) >> 2) & 0x0000FC)) );
}
function getBv3(b, bp) { return b + bp - 0x808080; }
function getCv3(c, cp) { return c + cp - 0x808080; }
function getAv3(a, bp, cp) { return a + 0x1010100 - bp - cp; }
function b3toB(bn, ap) {
	return 0x808080 + bn
			- (((ap&0x010000) == 0) ? (ap&0xFF0000) : (0x17F0000 - ((ap&0xFF0000)<<1)))
			- (((ap&0x000100) == 0) ? (ap&0x00FF00) : (0x0017F00 - ((ap&0x00FF00)<<1)))
			- (((ap&0x000001) == 0) ? (ap&0x0000FF) : (0x000017F - ((ap&0x0000FF)<<1)));
}
function c3toC(cn, ap) {
	return 0x808080 + cn
			- (((ap&0x010000) == 0) ? (0x1800000 - ((ap&0xFF0000)<<1)) : ((ap&0xFF0000) + 0x010000))
			- (((ap&0x000100) == 0) ? (0x0018000 - ((ap&0x00FF00)<<1)) : ((ap&0x00FF00) + 0x000100))
			- (((ap&0x000001) == 0) ? (0x0000180 - ((ap&0x0000FF)<<1)) : ((ap&0x0000FF) + 0x000001));
}

// for 1:2:4
function getA124(a, ad1, ad2, a1) {
	return (getA124x(((a>>16)&0xFF) * 3, (((ad1>>16)&0xFF) + ((ad2>>16)&0xFF) + ((a1>>16)&0xFF)), (a1>>16)&0xFF) << 16)
	     | (getA124x(((a>> 8)&0xFF) * 3, (((ad1>> 8)&0xFF) + ((ad2>> 8)&0xFF) + ((a1>> 8)&0xFF)), (a1>> 8)&0xFF) <<  8)
	     | (getA124x(((a>> 0)&0xFF) * 3, (((ad1>> 0)&0xFF) + ((ad2>> 0)&0xFF) + ((a1>> 0)&0xFF)), (a1>> 0)&0xFF) <<  0);
}
function getA124x(ax, nx, a1x) {
	return (Math.max(0, Math.min(0xF0, a1x + ax - nx)) & 0xF0) | (a1x & 0x0F);
}

function padRGB(value) {
	const str = pad0(Integer.toBinaryString(value), 24);
	return str.substring(0, 8) + " " + str.substring(8, 16) + " " + str.substring(16, 24);
}

/**
 * 1:1:4 레거시 형식 해석이 가능한지 패리티 검증
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.canPrototype = function(bmp) {
	console.info("\nis it prototype?");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 1:1:4 결합 이미지일 경우 크기는 짝수여야 함
	if (width % 2 > 0 || height % 2 > 0) {
		return false;
	};
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 2, height / 2);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		
		// (2x,2y)와 (2x+1,2y+1)은 둘 다 출력물 이미지의 원본 픽셀로 같은 값이어야 함
		if ((bmp.getRGB(2*x, 2*y  ) & 0xFFFFFF) != (bmp.getRGB(2*x+1, 2*y+1) & 0xFFFFFF)) {
			checkFailed = true;
			break;
		}
		
		// (2x+1,2y)와 (2x,2y+1)은 합쳐서 0xFFFFFF가 나와야 함
		if ((bmp.getRGB(2*x, 2*y+1) & 0xFFFFFF)  + (bmp.getRGB(2*x+1, 2*y  ) & 0xFFFFFF) != 0xFFFFFF) {
			checkFailed = true;
			break;
		}
	}
	if (checkFailed) {
		console.info("체크섬 오류 - 레거시 WithTarget 1:1:4 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - 레거시 WithTarget 1:1:4 형식 가능");
	return true;
}
/**
 * 1:1:4 형식 해석이 가능한지 패리티 검증
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.can114 = function(bmp) {
	console.info("\nis it 1:1:4?");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 1:1:4 결합 이미지일 경우 크기는 짝수여야 함
	if (width % 2 > 0 || height % 2 > 0) {
		return false;
	}
	
	let a, b, c, d;
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 2, height / 2);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		console.debug("sample(" + pad(x, 4) + ", " + pad(y, 4) + ")");
		
		// (2x,2y)와 (2x+1,2y+1)은 둘 다 출력물 이미지의 원본 픽셀로 같은 값이어야 함
		a = bmp.getRGB(2*x  , 2*y  );
		d = bmp.getRGB(2*x+1, 2*y+1);
		if (a != d) {
			console.info("a != d");
			checkFailed = true;
			break;
		}
		
		// 패리티 검증
		b = bmp.getRGB(2*x+1, 2*y  );
		c = bmp.getRGB(2*x  , 2*y+1);
		
		if (!isValid(a, b, c, getD(b, c))) {
			checkFailed = true;
			break;
		}
	}
	if (checkFailed) {
		console.info("체크섬 오류 - WithTarget 1:1:4 v1 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - WithTarget 1:1:4 v1 형식 가능");
	return true;
}
/**
 * 1:1:4 v2 형식 해석이 가능한지 패리티 검증
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.can114v2 = function(bmp) {
	console.info("\nis it 1:1:4 v2?");

	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 1:1:4 결합 이미지일 경우 크기는 짝수여야 함
	if (width % 2 > 0 || height % 2 > 0) {
		return false;
	}
	
	let a, bn, cn, an, ap, b, c;
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 2, height / 2);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		console.debug("sample(" + pad(x, 4) + ", " + pad(y, 4) + ")");
		
		a  = bmp.getRGB(2*x  , 2*y  );
		bn = bmp.getRGB(2*x+1, 2*y  );
		cn = bmp.getRGB(2*x  , 2*y+1);
		an = bmp.getRGB(2*x+1, 2*y+1);
		ap = getAp(a, an);
		
		b = b2toB(bn, ap);
		c = c2toC(cn, ap);
		
		if (!isValid(a, b, c, getD(b, c))) {
			checkFailed = true;
			break;
		}
	}
	if (checkFailed) {
		console.info("체크섬 오류 - WithTarget 1:1:4 v2 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - WithTarget 1:1:4 v2 형식 가능");
	return true;
}
/**
 * 1:1:4 v3 형식 해석이 가능한지 패리티 검증
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.can114v3 = function(bmp) {
	console.info("\nis it 1:1:4 v3?");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 1:1:4 결합 이미지일 경우 크기는 짝수여야 함
	if (width % 2 > 0 || height % 2 > 0) {
		return false;
	}
	
	let a, bn, cn, an, ap, b, c;
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 2, height / 2);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		console.debug("sample(" + pad(x, 4) + ", " + pad(y, 4) + ")");
		
		a  = bmp.getRGB(2*x  , 2*y  );
		bn = bmp.getRGB(2*x+1, 2*y  );
		cn = bmp.getRGB(2*x  , 2*y+1);
		an = bmp.getRGB(2*x+1, 2*y+1);
		ap = getAp(a, an);
		b = b3toB(bn, ap);
		c = c3toC(cn, ap);
		
		if (!isValid(a, b, c, getD(b, c))) {
			checkFailed = true;
			break;
		}
	}
	if (checkFailed) {
		console.info("체크섬 오류 - WithTarget 1:1:4 v3 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - WithTarget 1:1:4 v3 형식 가능");
	return true;
}
/**
 * 1:4:9 형식 해석이 가능한지 패리티 검증
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.can149 = function(bmp) {
	console.info("\nis it 1:4:9?");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 1:4:9 결합 이미지일 경우 크기는 3의 배수여야 함
	if (width % 3 > 0 || height % 3 > 0) {
		return false;
	}
	
	let b1, c1, b2
	  , c4, a , c2
	  , b4, c3, b3;
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 3, height / 3);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		console.debug("sample(" + pad(x, 4) + ", " + pad(y, 4) + "):");
		
		a  = bmp.getRGB(3*x+1, 3*y+1);
		b1 = bmp.getRGB(3*x  , 3*y  );
		c1 = bmp.getRGB(3*x+1, 3*y  );
		b2 = bmp.getRGB(3*x+2, 3*y  );
		c2 = bmp.getRGB(3*x+2, 3*y+1);
		b3 = bmp.getRGB(3*x+2, 3*y+2);
		c3 = bmp.getRGB(3*x+1, 3*y+2);
		b4 = bmp.getRGB(3*x  , 3*y+2);
		c4 = bmp.getRGB(3*x  , 3*y+1);
		
		// 패리티 검증
		if (!isValid(a, b1, c1, getD(b1, c1))) { checkFailed = true; break; }
		if (!isValid(a, b2, c2, getD(b2, c2))) { checkFailed = true; break; }
		if (!isValid(a, b3, c3, getD(b3, c3))) { checkFailed = true; break; }
		if (!isValid(a, b4, c4, getD(b4, c4))) { checkFailed = true; break; }
	}
	if (checkFailed) {
		console.info("체크섬 오류 - WithTarget 1:4:9 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - WithTarget 1:4:9 형식 가능");
	return true;
}
/**
 * 2:3:8 형식 해석이 가능한지 패리티 검증
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.can238 = function(bmp) {
	console.info("\nis it 2:3:8?");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 2:3:8 결합 이미지일 경우 크기는 6x4의 배수여야 함
	if (width % 6 > 0 || height % 4 > 0) {
		return false;
	}
	
	let b1, a1
	  , c1, b2
	  , a2, c2
	  , b3, c3, a3;
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 2, height / 4);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		console.debug("sample(" + pad(x, 4) + ", " + pad(y, 4) + "):");
		
		a1 = bmp.getRGB(2*x+1, 4*y  );
		a2 = bmp.getRGB(2*x  , 4*y+2);
		b1 = bmp.getRGB(2*x  , 4*y  );
		c1 = bmp.getRGB(2*x  , 4*y+1);
		b2 = bmp.getRGB(2*x+1, 4*y+1);
		c2 = bmp.getRGB(2*x+1, 4*y+2);
		b3 = bmp.getRGB(2*x  , 4*y+3);
		c3 = bmp.getRGB(2*x+1, 4*y+3);
		a3 = ((((a1&0xFF0000) + (a2&0xFF0000)) >> 1) & 0xFF0000)
		   | ((((a1&0x00FF00) + (a2&0x00FF00)) >> 1) & 0x00FF00)
		   | ((((a1&0x0000FF) + (a2&0x0000FF)) >> 1) & 0x0000FF);
		
		// 패리티 검증
		if (!isValid(a1, b1, c1, getD(b1, c1))) { checkFailed = true; break; }
		if (!isValid(a3, b2, c2, getD(b2, c2))) { checkFailed = true; break; }
		if (!isValid(a2, b3, c3, getD(b3, c3))) { checkFailed = true; break; }
	}
	if (checkFailed) {
		console.info("체크섬 오류 - WithTarget 2:3:8 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - WithTarget 2:3:8 형식 가능");
	return true;
}
/**
 * 4:2:9 형식 해석이 가능한지 패리티 검증
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.can429 = function(bmp) {
	console.info("\nis it 4:2:9?");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 4:2:9 결합 이미지일 경우 크기는 3의 배수여야 함
	if (width % 3 > 0 || height % 3 > 0) {
		return false;
	}
	
	let a1, b1, aA
	  , c1,aAB, c2
	  , aB, b2, a2;
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 3, height / 3);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		console.debug("sample(" + pad(x, 4) + ", " + pad(y, 4) + "):");
		
		a1 = bmp.getRGB(3*x  , 3*y  );
		aA = bmp.getRGB(3*x+2, 3*y  );
		aB = bmp.getRGB(3*x  , 3*y+2);
		a2 = bmp.getRGB(3*x+2, 3*y+2);
		aAB= bmp.getRGB(3*x+1, 3*y+1) & 0xFFFFFF;
		b1 = bmp.getRGB(3*x+1, 3*y  );
		c1 = bmp.getRGB(3*x  , 3*y+1);
		b2 = bmp.getRGB(3*x+1, 3*y+2);
		c2 = bmp.getRGB(3*x+2, 3*y+1);
		
		// 패리티 검증
		if (!isValid(a1, b1, c1, getD(b1, c1))) { checkFailed = true; break; }
		if (!isValid(a2, b2, c2, getD(b2, c2))) { checkFailed = true; break; }
		if (aAB != ( ((((aA&0xFF0000) + (aB&0xFF0000)) >> 1) & 0xFF0000)
		           | ((((aA&0x00FF00) + (aB&0x00FF00)) >> 1) & 0x00FF00)
		           | ((((aA&0x0000FF) + (aB&0x0000FF)) >> 1) & 0x0000FF) )) {
			console.debug("aAB != (aA+aB)/2");
			checkFailed = true; break;
		}
	}
	if (checkFailed) {
		console.info("체크섬 오류 - WithTarget 4:2:9 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - WithTarget 4:2:9 형식 가능");
	return true;
}
/**
 * 1:2:4 형식 해석이 가능한지 검증 (패리티 없음)
 * @param bmp
 * @return
 * @throws Exception
 */
WithTarget.can124 = function(bmp) {
	console.info("\nis it 1:2:4?");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	// 1:2:4 결합 이미지일 경우 크기는 2의 배수여야 함
	if (width % 2 > 0 || height % 2 > 0) {
		return false;
	}
	
	let a, ad1, ad2;
	
	let checkFailed = false;
	
	const points = getChecksumPoints(width / 2, height / 2);
	for (let i = 0; i < points.length; i++) {
		const x = points[i][0];
		const y = points[i][1];
		console.debug("sample(" + pad(x, 4) + ", " + pad(y, 4) + "):");

		a   = bmp.getRGB(2*x  , 2*y  );
		ad1 = bmp.getRGB(2*x+1, 2*y  );
		ad2 = bmp.getRGB(2*x  , 2*y+1);
		
		// 값 범위 검증, 별도 패리티 검사 없음
		if ((a&0xC0C0C0) != (ad1&0xC0C0C0)) { checkFailed = true; break; }
		if ((a&0xC0C0C0) != (ad2&0xC0C0C0)) { checkFailed = true; break; }
	}
	if (checkFailed) {
		console.info("체크섬 오류 - WithTarget 1:2:4 형식 이미지가 아님");
		return false;
	}
	
	console.info("체크섬 통과 - WithTarget 1:2:4 형식 가능");
	return true;
}
/**
 * 1:1:4 형식 비트맵 이미지를 해석
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap114 = async function(bmp, shift, xors) {
	console.info("\nWithTarget.fromBitmap 1:1:4");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	const xSize = width / 2, ySize = height / 2;
	
	let a, b, c;
	
	const targetImage = new BufferedImage(xSize, ySize);
	const dataImage   = new BufferedImage(xSize, ySize);
	
	for (let y = 0; y < ySize; y++) {
		for (let x = 0; x < xSize; x++) {
			a = bmp.getRGB(2*x  , 2*y  );
			b = bmp.getRGB(2*x+1, 2*y  );
			c = bmp.getRGB(2*x  , 2*y+1);
			
			targetImage.setRGB(x, y, a);
			dataImage  .setRGB(x, y, getD(b, c));
		}
	}
	
	let containers = await Container.fromBitmap(dataImage, shift, xors);
	if (containers.length) {
		return new WithTarget(targetImage, containers, TYPE_114v1);
	}
}
/**
 * 1:1:4 v2 형식 비트맵 이미지를 해석
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap114v2 = async function(bmp, shift, xors) {
	console.info("\nWithTarget.fromBitmap 1:1:4 v2");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	const xSize = width / 2, ySize = height / 2;
	
	let a, an, bn, cn, ap;
	
	const targetImage = new BufferedImage(xSize, ySize);
	const dataImage   = new BufferedImage(xSize, ySize);
	
	for (let y = 0; y < ySize; y++) {
		for (let x = 0; x < xSize; x++) {
			a  = bmp.getRGB(2*x  , 2*y  );
			bn = bmp.getRGB(2*x+1, 2*y  );
			cn = bmp.getRGB(2*x  , 2*y+1);
			an = bmp.getRGB(2*x+1, 2*y+1);
			ap = getAp(a, an);
			
			targetImage.setRGB(x, y, a);
			dataImage  .setRGB(x, y, getD(b2toB(bn, ap), c2toC(cn, ap)));
		}
	}
	
	let containers = await Container.fromBitmap(dataImage, shift, xors);
	if (containers.length) {
		return new WithTarget(targetImage, containers, TYPE_114v2);
	}
}
/**
 * 1:1:4 v3 형식 비트맵 이미지를 해석
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap114v3 = async function(bmp, shift, xors) {
	console.info("\nWithTarget.fromBitmap 1:1:4 v3");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	const xSize = width / 2, ySize = height / 2;
	
	let a, an, bn, cn, ap;
	
	const targetImage = new BufferedImage(xSize, ySize);
	const dataImage   = new BufferedImage(xSize, ySize);
	
	for (let y = 0; y < ySize; y++) {
		for (let x = 0; x < xSize; x++) {
			a  = bmp.getRGB(2*x  , 2*y  );
			bn = bmp.getRGB(2*x+1, 2*y  );
			cn = bmp.getRGB(2*x  , 2*y+1);
			an = bmp.getRGB(2*x+1, 2*y+1);
			ap = getAp(a, an);
			
			targetImage.setRGB(x, y, a);
			dataImage  .setRGB(x, y, getD(b3toB(bn, ap), c3toC(cn, ap)));
		}
	}
	
	let containers = await Container.fromBitmap(dataImage, shift, xors);
	if (containers.length) {
		return new WithTarget(targetImage, containers, TYPE_114v3);
	}
}
/**
 * 1:4:9 형식 비트맵 이미지를 해석
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap149 = (bmp, shift, xors) => {
	console.info("\nWithTarget.fromBitmap 1:4:9");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	const xSize = width / 3, ySize = height / 3;
	
	let b1, c1, b2
	  , c4, a , c2
	  , b4, c3, b3;
	
	const targetImage = new BufferedImage(xSize  , ySize  , BufferedImage.TYPE_3BYTE_BGR);
	const dataImage   = new BufferedImage(xSize*2, ySize*2, BufferedImage.TYPE_3BYTE_BGR);
	
	for (let y = 0; y < ySize; y++) {
		for (let x = 0; x < xSize; x++) {
			a  = bmp.getRGB(3*x+1, 3*y+1);
			b1 = bmp.getRGB(3*x  , 3*y  );
			c1 = bmp.getRGB(3*x+1, 3*y  );
			b2 = bmp.getRGB(3*x+2, 3*y  );
			c2 = bmp.getRGB(3*x+2, 3*y+1);
			b3 = bmp.getRGB(3*x+2, 3*y+2);
			c3 = bmp.getRGB(3*x+1, 3*y+2);
			b4 = bmp.getRGB(3*x  , 3*y+2);
			c4 = bmp.getRGB(3*x  , 3*y+1);
			
			targetImage.setRGB(x, y, a);
			dataImage.setRGB(2*x  , 2*y  , getD(b1, c1));
			dataImage.setRGB(2*x+1, 2*y  , getD(b2, c2));
			dataImage.setRGB(2*x+1, 2*y+1, getD(b3, c3));
			dataImage.setRGB(2*x  , 2*y+1, getD(b4, c4));
		}
	}
	
	const containers = Container.fromBitmap(dataImage, shift, xors);
	if (containers.length) {
		return new WithTarget(targetImage, containers, TYPE_149);
	}
}
/**
 * 2:3:8 형식 비트맵 이미지를 해석
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap238 = async function(bmp, shift, xors) {
	console.info("\nWithTarget.fromBitmap 2:3:8");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	const xSize = width / 2, ySize = height / 4;
	
	let b1, a1
	  , c1, b2
	  , a2, c2
	  , b3, c3;
	
	const targetImage = new BufferedImage(xSize, ySize*2, BufferedImage.TYPE_3BYTE_BGR);
	const dataImage   = new BufferedImage(xSize, ySize*3, BufferedImage.TYPE_3BYTE_BGR);
	
	for (let y = 0; y < ySize; y++) {
		for (let x = 0; x < xSize; x++) {
			a1 = bmp.getRGB(2*x+1, 4*y  );
			a2 = bmp.getRGB(2*x  , 4*y+2);
			b1 = bmp.getRGB(2*x  , 4*y  );
			c1 = bmp.getRGB(2*x  , 4*y+1);
			b2 = bmp.getRGB(2*x+1, 4*y+1);
			c2 = bmp.getRGB(2*x+1, 4*y+2);
			b3 = bmp.getRGB(2*x  , 4*y+3);
			c3 = bmp.getRGB(2*x+1, 4*y+3);
			
			targetImage.setRGB(x, 2*y  , a1);
			targetImage.setRGB(x, 2*y+1, a2);
			dataImage.setRGB(x, 3*y  , getD(b1, c1));
			dataImage.setRGB(x, 3*y+1, getD(b2, c2));
			dataImage.setRGB(x, 3*y+2, getD(b3, c3));
		}
	}
	
	let containers = await Container.fromBitmap(dataImage, shift, xors);
	if (containers.length) {
		return new WithTarget(targetImage, containers, TYPE_238);
	}
}
/**
 * 4:2:9 형식 비트맵 이미지를 해석
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap429 = async function(bmp, shift, xors) {
	console.info("\nWithTarget.fromBitmap 4:2:9");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	const xSize = width / 3, ySize = height / 3;
	
	let a1, b1, aA
	  , c1,     c2
	  , aB, b2, a2;
	
	const targetImage = new BufferedImage(xSize*2, ySize*2, BufferedImage.TYPE_3BYTE_BGR);
	const dataImage   = new BufferedImage(xSize  , ySize*2, BufferedImage.TYPE_3BYTE_BGR);
	
	for (let y = 0; y < ySize; y++) {
		for (let x = 0; x < xSize; x++) {
			a1 = bmp.getRGB(3*x  , 3*y  );
			aA = bmp.getRGB(3*x+2, 3*y  );
			aB = bmp.getRGB(3*x  , 3*y+2);
			a2 = bmp.getRGB(3*x+2, 3*y+2);
			b1 = bmp.getRGB(3*x+1, 3*y  );
			c1 = bmp.getRGB(3*x  , 3*y+1);
			b2 = bmp.getRGB(3*x+1, 3*y+2);
			c2 = bmp.getRGB(3*x+2, 3*y+1);

			targetImage.setRGB(2*x  , 2*y  , a1);
			targetImage.setRGB(2*x+1, 2*y  , aA);
			targetImage.setRGB(2*x  , 2*y+1, aB);
			targetImage.setRGB(2*x+1, 2*y+1, a2);
			dataImage.setRGB(x, 2*y  , getD(b1, c1));
			dataImage.setRGB(x, 2*y+1, getD(b2, c2));
		}
	}
	
	let containers = await Container.fromBitmap(dataImage, shift, xors);
	if (containers.length) {
		return new WithTarget(targetImage, containers, TYPE_429);
	}
}
/**
 * 1:2:4 형식 비트맵 이미지를 해석
 * @param bmp
 * @param shift: 출력물 픽셀 밀기
 * @param xors: 출력물 xor 연산 수행
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap124 = async function(bmp, shift, xors) {
	console.info("\nWithTarget.fromBitmap 1:2:4");
	
	const width  = bmp.getWidth();
	const height = bmp.getHeight();
	console.info("input size: " + width + " x " + height);
	
	const xSize = width / 2, ySize = height / 2;
	
	let a, ad1, ad2, a1;
	
	const targetImage = new BufferedImage(xSize, ySize  , BufferedImage.TYPE_3BYTE_BGR);
	const dataImage   = new BufferedImage(xSize, ySize*2, BufferedImage.TYPE_3BYTE_BGR);
	
	for (let y = 0; y < ySize; y++) {
		for (let x = 0; x < xSize; x++) {
			a   = bmp.getRGB(2*x  , 2*y  );
			ad1 = bmp.getRGB(2*x+1, 2*y  );
			ad2 = bmp.getRGB(2*x  , 2*y+1);
			a1  = bmp.getRGB(2*x+1, 2*y+1);
			
			targetImage.setRGB(x  , y  , a);
			dataImage.setRGB(x, 2*y  , ((a1<<4)&0xC0C0C0) | (ad1&0x3F3F3F));
			dataImage.setRGB(x, 2*y+1, ((a1<<6)&0xC0C0C0) | (ad2&0x3F3F3F));
		}
	}
	
	let containers = await Container.fromBitmap(dataImage, shift, xors);
	if (containers.length) {
		return new WithTarget(targetImage, containers, TYPE_124);
	}
}
/**
 * 비트맵 이미지를 주어진 값에 따라 변환 후, 주어진 알고리즘으로 차례로 해석 시도
 *
 * @param bmp
 * @param possibility
 * @param shift
 * @param xors
 * @return
 * @throws Exception
 */
WithTarget.fromBitmap = async function(bmp, possibility, shift=0, xors=[]) {
	console.info("\nWithTarget.fromBitmap");
	let result = null;
	
	// 1:1:4 v3 형식으로 시도
	if (((possibility & CAN_114v3) > 0) && (result = await WithTarget.fromBitmap114v3(bmp, shift, xors)) != null) {
		return result;
	}
	// 1:1:4 v2 형식으로 시도
	if (((possibility & CAN_114v2) > 0) && (result = await WithTarget.fromBitmap114v2(bmp, shift, xors)) != null) {
		return result;
	}
	// 1:1:4 v1 형식으로 시도
	if (((possibility & CAN_114v1) > 0) && (result = await WithTarget.fromBitmap114(bmp, shift, xors)) != null) {
		return result;
	}
	// 1:4:9 형식으로 시도
	if (((possibility & CAN_149) > 0) && (result = await WithTarget.fromBitmap149(bmp, shift, xors)) != null) {
		return result;
	}
	// 2:3:8 형식으로 시도
	if (((possibility & CAN_238) > 0) && (result = await WithTarget.fromBitmap238(bmp, shift, xors)) != null) {
		return result;
	}
	// 4:2:9 형식으로 시도
	if (((possibility & CAN_429) > 0) && (result = await WithTarget.fromBitmap429(bmp, shift, xors)) != null) {
		return result;
	}
	// 1:2:4 형식으로 시도
	if (((possibility & CAN_124) > 0) && (result = await WithTarget.fromBitmap124(bmp, shift, xors)) != null) {
		return result;
	}
	
	console.info("Without target 해석 시도");
	let containers = await Container.fromBitmap(bmp, shift, xors);
	if (containers.length) {
		return new WithTarget(null, containers, 0);
	}
	
	return null;
}
