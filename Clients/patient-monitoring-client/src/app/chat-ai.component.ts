import { Component } from '@angular/core';

@Component({
  selector: 'app-chat-ai',
  templateUrl: './chat-ai.component.html',
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
