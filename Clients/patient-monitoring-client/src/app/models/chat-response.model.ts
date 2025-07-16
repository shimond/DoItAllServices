export interface ChatResponse {
  response: {
    messages: ChatMessage[];
    responseId: string;
    conversationId: string | null;
    modelId: string;
    createdAt: string;
    finishReason: string;
    usage: ChatUsage;
    additionalProperties?: any;
  };
}

export interface ChatMessage {
  authorName: string | null;
  role: 'assistant' | 'user';
  contents: ChatContent[];
  messageId: string;
  additionalProperties?: any;
}

export interface ChatContent {
  $type: string;
  text: string;
  additionalProperties?: any;
}

export interface ChatUsage {
  inputTokenCount: number;
  outputTokenCount: number;
  totalTokenCount: number;
  additionalCounts?: any;
}
