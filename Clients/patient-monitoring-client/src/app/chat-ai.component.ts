import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';

@Component({
  selector: 'app-chat-ai',
  templateUrl: './chat-ai.component.html',
  standalone:true,
  imports: [CommonModule, MatFormFieldModule, MatCardModule, FormsModule],
  styleUrls: ['./chat-ai.component.scss']
})
export class ChatAiComponent {
  chatHistory: { sender: 'user' | 'ai'; text: string }[] = [];
  chatInput: string = '';

  sendChat() {
    if (!this.chatInput.trim()) return;
    this.chatHistory.push({ sender: 'user', text: this.chatInput });
    // TODO: Call backend AI API here and push AI response
    this.chatHistory.push({ sender: 'ai', text: 'AI response goes here (connect to backend).' });
    this.chatInput = '';
  }
}
