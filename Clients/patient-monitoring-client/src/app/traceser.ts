import { EnvironmentProviders, provideAppInitializer } from '@angular/core';
import { BatchSpanProcessor, SimpleSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { ConsoleSpanExporter } from '@opentelemetry/sdk-trace-web';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
export function provideInstrumentation(): EnvironmentProviders {
  return provideAppInitializer(() => {
    // Configure our resource
    const resource = resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'Angular App',
      [ATTR_SERVICE_VERSION]: '1.0.0',
    });

    const provider = new WebTracerProvider({
      resource,
      spanProcessors: [
      new BatchSpanProcessor(
        new OTLPTraceExporter({
          url: `${window.origin}/v1/traces`,
        }),
      )
      ],

    });


    provider.register({
      contextManager: new ZoneContextManager(),
    }); // Register instrumentations to automatically capture traces from

    registerInstrumentations({
      instrumentations: [
        getWebAutoInstrumentations(),
      ],
    });
  });
}