// fontEngine.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"
#include "fontEngine.h"

BOOL APIENTRY DllMain( HANDLE hModule, 
                       DWORD  ul_reason_for_call, 
                       LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
    return TRUE;
}

#define MaxNumfontVertices 600
#define MAX_FONTS			20

// A structure for our custom vertex type
struct CUSTOMVERTEX
{
    FLOAT x, y, z, rhw; // The transformed position for the vertex
    DWORD color;        // The vertex color
    FLOAT       tu, tv;   // The texture coordinates
};

// Our custom FVF, which describes our custom vertex structure
#define D3DFVF_CUSTOMVERTEX (D3DFVF_XYZRHW|D3DFVF_DIFFUSE|D3DFVF_TEX1)


struct FONT_DATA_T
{
	int						iFirstChar;
	int						iEndChar;
	float					fTextureScale;
	float					fTextureWidth;
	float					fTextureHeight;
	int     				iMaxVertices;
	float   				fSpacingPerChar;
	LPDIRECT3DTEXTURE9		pTexture;
	LPDIRECT3DVERTEXBUFFER9	pVertexBuffer;
	float					textureCoord[MaxNumfontVertices][4];
} ;

static FONT_DATA_T*			fontData = new FONT_DATA_T[MAX_FONTS];
static LPDIRECT3DDEVICE9	m_pDevice=NULL;	

void AddFont(void* device, int fontNumber,void* fontTexture, int firstChar, int endChar, float textureScale, float textureWidth, float textureHeight, float fSpacingPerChar,int maxVertices)
{
	if (fontNumber< 0 || fontNumber>=MAX_FONTS) return;
	if (fontTexture==NULL) return;
	if (firstChar<0 || firstChar>endChar) return;
	m_pDevice=(LPDIRECT3DDEVICE9)device;
	D3DCAPS9 caps;
	int mem=m_pDevice->GetAvailableTextureMem();
	m_pDevice->GetDeviceCaps(&caps);


	fontData[fontNumber].iFirstChar    = firstChar;
	fontData[fontNumber].iEndChar      = endChar;
	fontData[fontNumber].fTextureScale = textureScale;
	fontData[fontNumber].fTextureWidth = textureWidth;
	fontData[fontNumber].fTextureHeight= textureHeight;
	fontData[fontNumber].iMaxVertices  = maxVertices;
	fontData[fontNumber].pTexture      = (LPDIRECT3DTEXTURE9)fontTexture;
	fontData[fontNumber].fSpacingPerChar = fSpacingPerChar;

	LPDIRECT3DVERTEXBUFFER9 g_pVB        = NULL;
	int hr=m_pDevice->CreateVertexBuffer(	maxVertices*sizeof(CUSTOMVERTEX),
											0, D3DFVF_CUSTOMVERTEX,
											D3DPOOL_DEFAULT, 
											&g_pVB, 
											NULL) ;
	fontData[fontNumber].pVertexBuffer=g_pVB;
	int x=123;
}

void SetCoordinate(int fontNumber, int index, int subindex, float fValue)
{
	if (fontNumber< 0 || fontNumber>=MAX_FONTS) return;
	if (index < 0     || index > MaxNumfontVertices) return;
	if (subindex < 0  || subindex > 3) return;
	fontData[fontNumber].textureCoord[index][subindex]=fValue;
}

void DrawText3D(int fontNumber, char* text, int xposStart, int yposStart, DWORD intColor)
{
	if (fontNumber< 0 || fontNumber>=MAX_FONTS) return;
	if (m_pDevice==NULL) return;
	if (fontData[fontNumber].pVertexBuffer==NULL) return;

	float xpos = (float)xposStart;
	float ypos = (float)yposStart;
	xpos -= fontData[fontNumber].fSpacingPerChar;
	xpos-=0.5f;
	float fStartX = xpos;
	ypos -=0.5f;

	float yoff    = (fontData[fontNumber].textureCoord[0][3]-fontData[fontNumber].textureCoord[0][1])*fontData[fontNumber].fTextureHeight;
	float fScaleX = fontData[fontNumber].fTextureWidth  / fontData[fontNumber].fTextureScale;
	float fScaleY = fontData[fontNumber].fTextureHeight / fontData[fontNumber].fTextureScale;
	float fSpacing= 2 * fontData[fontNumber].fSpacingPerChar;

	CUSTOMVERTEX* pVertices=NULL;
    fontData[fontNumber].pVertexBuffer->Lock( 0, 0, (void**)&pVertices, 0 ) ;

	int dwNumTriangles=0;
	int iv=0;
	
	for (int i=0; i < (int)strlen(text);++i)
	{
        char c=text[i];
		if (c == '\n')
		{
			xpos = fStartX;
			ypos += yoff;
		}

		if (c < fontData[fontNumber].iFirstChar || c >= fontData[fontNumber].iEndChar )
			continue;

        int index=c-fontData[fontNumber].iFirstChar;
		float tx1 = fontData[fontNumber].textureCoord[index][0];
		float ty1 = fontData[fontNumber].textureCoord[index][1];
		float tx2 = fontData[fontNumber].textureCoord[index][2];
		float ty2 = fontData[fontNumber].textureCoord[index][3];

		float w = (tx2-tx1) * fScaleX;
		float h = (ty2-ty1) * fScaleY;

		if (xpos<0 || xpos+2 > 768 ||
			ypos<0 || ypos+h > 576+100)
		{
			c=' ';
		}

		if (c != ' ')
		{
			float xpos2=xpos+w;
			float ypos2=ypos+h;
			pVertices[iv].rhw=1.0f;  pVertices[iv].z=0.0f;  pVertices[iv].x=xpos ;  pVertices[iv].y=ypos2 ; pVertices[iv].color=intColor;pVertices[iv].tu=tx1; pVertices[iv].tv=ty2;iv++;
			pVertices[iv].rhw=1.0f;  pVertices[iv].z=0.0f;  pVertices[iv].x=xpos ;  pVertices[iv].y=ypos  ; pVertices[iv].color=intColor;pVertices[iv].tu=tx1; pVertices[iv].tv=ty1;iv++;
			pVertices[iv].rhw=1.0f;  pVertices[iv].z=0.0f;  pVertices[iv].x=xpos2;  pVertices[iv].y=ypos2 ; pVertices[iv].color=intColor;pVertices[iv].tu=tx2; pVertices[iv].tv=ty2;iv++;
			pVertices[iv].rhw=1.0f;  pVertices[iv].z=0.0f;  pVertices[iv].x=xpos2;  pVertices[iv].y=ypos  ; pVertices[iv].color=intColor;pVertices[iv].tu=tx2; pVertices[iv].tv=ty1;iv++;
			pVertices[iv].rhw=1.0f;  pVertices[iv].z=0.0f;  pVertices[iv].x=xpos2;  pVertices[iv].y=ypos2 ; pVertices[iv].color=intColor;pVertices[iv].tu=tx2; pVertices[iv].tv=ty2;iv++;
			pVertices[iv].rhw=1.0f;  pVertices[iv].z=0.0f;  pVertices[iv].x=xpos ;  pVertices[iv].y=ypos  ; pVertices[iv].color=intColor;pVertices[iv].tu=tx1; pVertices[iv].tv=ty1;iv++;

			dwNumTriangles += 2;
			if (iv > (MaxNumfontVertices-12))
			{
				fontData[fontNumber].pVertexBuffer->Unlock();
				m_pDevice->SetTexture(0, fontData[fontNumber].pTexture);
				m_pDevice->SetFVF( D3DFVF_CUSTOMVERTEX );
				m_pDevice->SetStreamSource(0, fontData[fontNumber].pVertexBuffer, 0, sizeof(CUSTOMVERTEX) );
				m_pDevice->DrawPrimitive(D3DPT_TRIANGLELIST, 0, dwNumTriangles);
				m_pDevice->SetTexture(0, NULL);
				dwNumTriangles = 0;
				iv = 0;
				fontData[fontNumber].pVertexBuffer->Lock( 0, 0, (void**)&pVertices, 0 ) ;
			}
		}

		xpos += w - fSpacing;
	}

	if (iv > 0)
	{
		fontData[fontNumber].pVertexBuffer->Unlock();
		m_pDevice->SetTexture(0, fontData[fontNumber].pTexture);
		m_pDevice->SetFVF( D3DFVF_CUSTOMVERTEX );
		m_pDevice->SetStreamSource(0, fontData[fontNumber].pVertexBuffer, 0, sizeof(CUSTOMVERTEX) );
		m_pDevice->DrawPrimitive(D3DPT_TRIANGLELIST, 0, dwNumTriangles);
		m_pDevice->SetTexture(0, NULL);
		dwNumTriangles = 0;
		iv = 0;
	}
	else
	{
		fontData[fontNumber].pVertexBuffer->Unlock();
	}
}