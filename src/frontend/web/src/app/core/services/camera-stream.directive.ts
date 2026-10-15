import { Directive, ElementRef, effect, input } from '@angular/core';

/** Bind MediaStream to an HTML video element (srcObject is not an HTML attribute). */
@Directive({ selector: 'video[cameraStream]', standalone: true })
export class CameraStreamDirective {
  readonly cameraStream = input<MediaStream | null>(null);

  constructor(element: ElementRef<HTMLVideoElement>) {
    effect(() => {
      element.nativeElement.srcObject = this.cameraStream();
    });
  }
}
