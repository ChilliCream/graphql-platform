declare module '@mdx-js/react' {
    import * as React from 'react'
    type ComponentType =
        | 'a'
        | 'blockquote'
        | 'code'
        | 'del'
        | 'em'
        | 'h1'
        | 'h2'
        | 'h3'
        | 'h4'
        | 'h5'
        | 'h6'
        | 'hr'
        | 'img'
        | 'inlineCode'
        | 'li'
        | 'ol'
        | 'p'
        | 'pre'
        | 'strong'
        | 'sup'
        | 'table'
        | 'td'
        | 'thematicBreak'
        | 'tr'
        | 'ul'
    export interface MDXProviderProps {
        children: React.ReactNode
        components: Record<any, React.ComponentType<any>>
    }
    export class MDXProvider extends React.Component<MDXProviderProps> { }
}