import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { HttpClient } from '@angular/common/http';
// New response type for chat API
interface ChatApiResponse {
  response: string;
  conversationMessage: { role: 'assistant' | 'user', content: string };
  userMessage: { role: 'assistant' | 'user', content: string };
  patientId?: number;
}
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
    selector: 'app-chat-ai',
    templateUrl: './chat-ai.component.html',
    imports: [CommonModule, MatFormFieldModule,
        MatButtonModule,
        MatCardModule, FormsModule, MatInputModule
    ],
    styleUrls: ['./chat-ai.component.scss']
})
export class ChatAiComponent {
  chatHistory: { sender: 'user' | 'ai'; text: string; createdAt?: string }[] = [];
  chatInput: string = '';

  constructor(private http: HttpClient) {}

  sendChat() {
    const input = this.chatInput.trim();
    if (!input) return;
    this.chatHistory.push({ sender: 'user', text: input, createdAt: new Date().toISOString() });

    // Build conversation history for the backend
    const conversationHistory = this.chatHistory.slice(0, -1).map(msg => ({
      role: msg.sender === 'user' ? 'user' : 'assistant',
      content: msg.text
    }));

    this.http.post<ChatApiResponse>('/api/chat', {
      Question: input,
      ConversationHistory: conversationHistory
    }).subscribe({
      next: (res) => {
        // Add the assistant's response to the chat history
        if (res.conversationMessage) {
          this.chatHistory.push({
            sender: res.conversationMessage.role === 'assistant' ? 'ai' : 'user',
            text: res.conversationMessage.content,
            createdAt: new Date().toISOString()
          });
        }
      },
      error: () => {
        this.chatHistory.push({ sender: 'ai', text: 'Sorry, there was an error contacting the AI.', createdAt: new Date().toISOString() });
      }
    });
    this.chatInput = '';
  }
}
