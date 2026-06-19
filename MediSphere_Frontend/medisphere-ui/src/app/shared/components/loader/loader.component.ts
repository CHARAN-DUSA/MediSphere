import { Component, Input } from '@angular/core';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [NgIf],
  templateUrl: './loader.html',
  styleUrls: ['./loader.css']
})
export class LoaderComponent {
  @Input() loading = false;
  @Input() text = '';
}
